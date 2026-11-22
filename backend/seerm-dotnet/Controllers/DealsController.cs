using Microsoft.AspNetCore.Mvc;
using SeeRM.Api.Contracts;
using SeeRM.Api.Services;

namespace SeeRM.Api.Controllers;

/// <summary>Sales opportunities. Create and list are under /api/customers/{customerId}/deals.</summary>
[ApiController]
[Route("api/deals")]
[Produces("application/json")]
public class DealsController(DealService deals) : ControllerBase
{
    /// <summary>Get one deal.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<DealDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<DealDto> Get(long id, CancellationToken ct) => deals.GetAsync(id, ct);

    /// <summary>Update a deal. Moving to won/lost sets closedAt; reopening clears it.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<DealDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<DealDto> Update(long id, DealRequest request, CancellationToken ct) => deals.UpdateAsync(id, request, ct);

    /// <summary>Delete a deal. Linked tasks are kept and unlinked.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await deals.DeleteAsync(id, ct);
        return NoContent();
    }
}
