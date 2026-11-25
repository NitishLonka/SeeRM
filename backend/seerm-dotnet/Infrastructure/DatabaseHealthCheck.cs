using Microsoft.Extensions.Diagnostics.HealthChecks;
using SeeRM.Api.Data;

namespace SeeRM.Api.Infrastructure;

/// <summary>Reports Unhealthy when the Customer database can't be reached.</summary>
public sealed class DatabaseHealthCheck(SeeRmDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        await db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy("Customer database reachable.")
            : HealthCheckResult.Unhealthy("Customer database unreachable.");
}
