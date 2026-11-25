using Microsoft.AspNetCore.Mvc;
using SeeRM.Leads.Contracts;
using SeeRM.Leads.Services;

namespace SeeRM.Leads.Controllers;

/// <summary>Prospects captured before they become customers.</summary>
[ApiController]
[Route("api/leads")]
[Produces("application/json")]
public class LeadsController(LeadService leads, LeadConversionService conversion) : ControllerBase
{
    /// <summary>
    /// Convert a Qualified lead into a customer in the Customer service.
    /// Links to an existing customer with the same email instead of creating a duplicate, and carries the
    /// estimated value over as an open deal. Safe to retry. Returns 503 (lead unchanged) if the Customer service is down.
    /// </summary>
    [HttpPost("{id:long}/convert")]
    [ProducesResponseType<ConvertLeadResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public Task<ConvertLeadResultDto> Convert(long id, CancellationToken ct) => conversion.ConvertAsync(id, ct);

    /// <summary>List leads, newest first.</summary>
    /// <param name="search">Case-insensitive match on name, company or email.</param>
    /// <param name="status">New | Contacted | Qualified | Unqualified | Converted</param>
    /// <param name="source">website | referral | event | cold_call | social | other</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">1–100, default 20.</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType<PagedResult<LeadDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<LeadDto>> List(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? source,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        leads.ListAsync(search, status, source, page, pageSize, ct);

    /// <summary>KPI values for the Leads screen.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<LeadSummaryDto>(StatusCodes.Status200OK)]
    public Task<LeadSummaryDto> Summary(CancellationToken ct) => leads.GetSummaryAsync(ct);

    /// <summary>Get one lead.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<LeadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<LeadDto> Get(long id, CancellationToken ct) => leads.GetAsync(id, ct);

    /// <summary>Create a lead.</summary>
    [HttpPost]
    [ProducesResponseType<LeadDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LeadDto>> Create(LeadRequest request, CancellationToken ct)
    {
        var created = await leads.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Update a lead. Converted leads are read-only (409).</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<LeadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<LeadDto> Update(long id, LeadRequest request, CancellationToken ct) => leads.UpdateAsync(id, request, ct);

    /// <summary>Delete a lead. Does not affect any customer it was converted to.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await leads.DeleteAsync(id, ct);
        return NoContent();
    }
}
