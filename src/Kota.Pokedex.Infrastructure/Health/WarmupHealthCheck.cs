using Kota.Pokedex.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kota.Pokedex.Infrastructure.Health;

public sealed class WarmupHealthCheck(IWarmupState warmupState) : IHealthCheck {
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(
            warmupState.IsComplete
                ? HealthCheckResult.Healthy("Startup prefetch complete.")
                : HealthCheckResult.Unhealthy("Startup prefetch still running."));
}
