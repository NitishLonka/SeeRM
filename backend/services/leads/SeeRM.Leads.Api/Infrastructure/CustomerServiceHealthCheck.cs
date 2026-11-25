using Microsoft.Extensions.Diagnostics.HealthChecks;
using SeeRM.Leads.Integration;

namespace SeeRM.Leads.Infrastructure;

/// <summary>
/// Reports Degraded (not Unhealthy) when the Customer service is down: lead CRUD still works,
/// only conversion is unavailable.
/// </summary>
public sealed class CustomerServiceHealthCheck(CustomerServiceClient customers) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        await customers.IsHealthyAsync(ct)
            ? HealthCheckResult.Healthy("Customer service reachable.")
            : HealthCheckResult.Degraded("Customer service unreachable; lead conversion unavailable.");
}
