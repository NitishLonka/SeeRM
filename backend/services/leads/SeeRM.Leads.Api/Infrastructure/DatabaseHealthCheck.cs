using Microsoft.Extensions.Diagnostics.HealthChecks;
using SeeRM.Leads.Data;

namespace SeeRM.Leads.Infrastructure;

/// <summary>Reports Unhealthy when the Leads database can't be reached.</summary>
public sealed class DatabaseHealthCheck(LeadsDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        await db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy("Leads database reachable.")
            : HealthCheckResult.Unhealthy("Leads database unreachable.");
}
