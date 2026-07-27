using Kota.Pokedex.Core.Options;
using Microsoft.Extensions.Options;

namespace Kota.Pokedex.Infrastructure.ExternalServices.PokeApi;

/// <summary>
/// Acquires the shared PokeAPI concurrency slot per HTTP attempt (P2.6),
/// so resilience retries release the semaphore between attempts.
/// </summary>
public sealed class PokeApiThrottleHandler : DelegatingHandler {
    private static readonly object ThrottleLock = new();
    private static SemaphoreSlim? _sharedThrottle;

    private readonly SemaphoreSlim _throttle;

    public PokeApiThrottleHandler(IOptions<PokeApiOptions> options) {
        _throttle = GetOrCreateThrottle(options.Value.MaxConcurrentRequests);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) {
        await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally {
            _throttle.Release();
        }
    }

    private static SemaphoreSlim GetOrCreateThrottle(int maxConcurrentRequests) {
        if (_sharedThrottle is not null) {
            return _sharedThrottle;
        }

        lock (ThrottleLock) {
            return _sharedThrottle ??= new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }
    }
}
