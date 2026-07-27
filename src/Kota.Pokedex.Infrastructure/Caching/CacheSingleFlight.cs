using System.Collections.Concurrent;

namespace Kota.Pokedex.Infrastructure.Caching;

/// <summary>
/// Shared in-process single-flight for cache miss factories (P1.1).
/// Used by both Memory and Redis ICacheService implementations so stampede logic is not duplicated.
/// </summary>
internal static class CacheSingleFlight {
    private static readonly ConcurrentDictionary<string, Task> Inflight = new(StringComparer.Ordinal);

    public static async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> getAsync,
        Func<T, TimeSpan?, CancellationToken, Task> setAsync,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry,
        CancellationToken cancellationToken) {
        var cached = await getAsync(cancellationToken).ConfigureAwait(false);
        if (cached is not null) {
            return cached;
        }

        var created = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var winner = Inflight.GetOrAdd(key, created.Task);

        if (!ReferenceEquals(winner, created.Task)) {
            return await ((Task<T>)winner).WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        try {
            cached = await getAsync(cancellationToken).ConfigureAwait(false);
            if (cached is not null) {
                created.SetResult(cached);
                return cached;
            }

            var value = await factory(cancellationToken).ConfigureAwait(false);
            await setAsync(value, expiry, cancellationToken).ConfigureAwait(false);
            created.SetResult(value);
            return value;
        }
        catch (Exception ex) {
            created.TrySetException(ex);
            throw;
        }
        finally {
            Inflight.TryRemove(key, out _);
        }
    }
}
