using Kota.Pokedex.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kota.Pokedex.Infrastructure.Health;

/// <summary>
/// Readiness dependency: SQLite / EF can connect (P1.6).
/// </summary>
public sealed class SqliteDbHealthCheck(AppDbContext dbContext) : IHealthCheck {
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        try {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQLite database is reachable.")
                : HealthCheckResult.Unhealthy("SQLite database is not reachable.");
        }
        catch (Exception ex) {
            return HealthCheckResult.Unhealthy("SQLite database check failed.", ex);
        }
    }
}
