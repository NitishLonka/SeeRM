using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SeeRM.Gateway;

/// <summary>
/// Calls a downstream service's /health endpoint. A failing service makes the gateway Degraded,
/// not Unhealthy: the gateway still routes traffic to the services that are up.
/// </summary>
public sealed class DownstreamHealthCheck(IHttpClientFactory httpFactory, string baseAddress) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var http = httpFactory.CreateClient("downstream-health");
        try
        {
            using var response = await http.GetAsync(new Uri(new Uri(baseAddress), "health"), ct);
            var body = (await response.Content.ReadAsStringAsync(ct)).Trim();
            return response.IsSuccessStatusCode && body == "Healthy"
                ? HealthCheckResult.Healthy(baseAddress)
                : HealthCheckResult.Degraded($"{baseAddress} reported {(int)response.StatusCode} {body}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Degraded($"{baseAddress} unreachable");
        }
    }
}
