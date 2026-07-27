using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kota.Pokedex.Infrastructure.Health;

/// <summary>
/// Readiness dependency: Redis (IDistributedCache) is reachable (P1.6).
/// </summary>
public sealed class RedisCacheHealthCheck(IDistributedCache cache) : IHealthCheck {
    private static readonly byte[] ProbePayload = [1];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        const string key = "__pokedex_health_probe";
        try {
            await cache.SetAsync(
                key,
                ProbePayload,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
                cancellationToken);
            var value = await cache.GetAsync(key, cancellationToken);
            return value is { Length: > 0 }
                ? HealthCheckResult.Healthy("Redis cache is reachable.")
                : HealthCheckResult.Unhealthy("Redis cache probe returned empty.");
        }
        catch (Exception ex) {
            return HealthCheckResult.Unhealthy("Redis cache check failed.", ex);
        }
    }
}
