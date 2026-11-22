using Microsoft.AspNetCore.Mvc;
using SeeRM.Api.Contracts;
using SeeRM.Api.Services;

namespace SeeRM.Api.Controllers;

/// <summary>Dashboard KPI cards.</summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    /// <summary>Total customers, active leads, pipeline revenue, task completion and follow-ups due in the next 7 days.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public Task<DashboardSummaryDto> Summary(CancellationToken ct) => dashboard.GetSummaryAsync(ct);
}
