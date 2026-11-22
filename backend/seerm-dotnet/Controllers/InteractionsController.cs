using Microsoft.AspNetCore.Mvc;
using SeeRM.Api.Contracts;
using SeeRM.Api.Services;

namespace SeeRM.Api.Controllers;

/// <summary>Calls, emails, meetings and notes. Create and list are under /api/customers/{customerId}/interactions.</summary>
[ApiController]
[Route("api/interactions")]
[Produces("application/json")]
public class InteractionsController(InteractionService interactions) : ControllerBase
{
    /// <summary>Get one interaction.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<InteractionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<InteractionDto> Get(long id, CancellationToken ct) => interactions.GetAsync(id, ct);

    /// <summary>Edit an interaction.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<InteractionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<InteractionDto> Update(long id, InteractionRequest request, CancellationToken ct) =>
        interactions.UpdateAsync(id, request, ct);

    /// <summary>Delete an interaction.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await interactions.DeleteAsync(id, ct);
        return NoContent();
    }
}
