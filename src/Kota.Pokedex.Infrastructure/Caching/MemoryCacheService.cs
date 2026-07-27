using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Kota.Pokedex.Infrastructure.Caching;

/// <summary>
/// In-process cache storing object graphs directly (P1.2) — no JSON round-trip.
/// </summary>
public class MemoryCacheService : ICacheService {
    private readonly IMemoryCache _memoryCache;
    private readonly CacheOptions _options;

    public MemoryCacheService(IMemoryCache memoryCache, IOptions<CacheOptions> options) {
        _memoryCache = memoryCache;
        _options = options.Value;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) {
        if (_memoryCache.TryGetValue(key, out T? value)) {
            return Task.FromResult(value);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) {
        var ttl = expiry ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);
        _memoryCache.Set(key, value, ttl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) =>
        CacheSingleFlight.GetOrCreateAsync(
            key,
            ct => GetAsync<T>(key, ct),
            (value, ttl, ct) => SetAsync(key, value, ttl, ct),
            factory,
            expiry,
            cancellationToken);
}
