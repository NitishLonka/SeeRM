using Microsoft.AspNetCore.Mvc;
using SeeRM.Api.Contracts;
using SeeRM.Api.Services;

namespace SeeRM.Api.Controllers;

/// <summary>Customer directory and the New Customer form.</summary>
[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public class CustomersController(CustomerService customers, DealService deals, TaskService tasks, InteractionService interactions)
    : ControllerBase
{
    /// <summary>List customers for the dashboard directory.</summary>
    /// <param name="search">Case-insensitive match on name or company.</param>
    /// <param name="status">Lead | Active | Inactive</param>
    /// <param name="email">Exact, case-insensitive email match. Used by other services (e.g. Leads) to find an existing customer.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">1–100, default 20.</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType<PagedResult<CustomerListItemDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<CustomerListItemDto>> List(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? email,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        customers.ListAsync(search, status, email, page, pageSize, ct);

    /// <summary>Get one customer.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CustomerDto> Get(long id, CancellationToken ct) => customers.GetAsync(id, ct);

    /// <summary>Create a customer. Status defaults to "Lead".</summary>
    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Create(CustomerRequest request, CancellationToken ct)
    {
        var created = await customers.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Update a customer. Omitting status leaves it unchanged.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<CustomerDto> Update(long id, CustomerRequest request, CancellationToken ct) =>
        customers.UpdateAsync(id, request, ct);

    /// <summary>Delete a customer and all of its deals, tasks and interactions.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await customers.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---- Child collections -------------------------------------------------

    /// <summary>List a customer's deals.</summary>
    [HttpGet("{customerId:long}/deals")]
    [ProducesResponseType<IReadOnlyList<DealDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<DealDto>> ListDeals(long customerId, CancellationToken ct) =>
        deals.ListForCustomerAsync(customerId, ct);

    /// <summary>Create a deal for a customer.</summary>
    [HttpPost("{customerId:long}/deals")]
    [ProducesResponseType<DealDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DealDto>> CreateDeal(long customerId, DealRequest request, CancellationToken ct)
    {
        var created = await deals.CreateAsync(customerId, request, ct);
        return CreatedAtAction(nameof(DealsController.Get), "Deals", new { id = created.Id }, created);
    }

    /// <summary>Create a task for a customer.</summary>
    [HttpPost("{customerId:long}/tasks")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> CreateTask(long customerId, TaskRequest request, CancellationToken ct)
    {
        var created = await tasks.CreateAsync(customerId, request, ct);
        return CreatedAtAction(nameof(TasksController.Get), "Tasks", new { id = created.Id }, created);
    }

    /// <summary>List a customer's interactions, newest first.</summary>
    [HttpGet("{customerId:long}/interactions")]
    [ProducesResponseType<IReadOnlyList<InteractionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<InteractionDto>> ListInteractions(long customerId, CancellationToken ct) =>
        interactions.ListForCustomerAsync(customerId, ct);

    /// <summary>Log a call, email, meeting or note for a customer.</summary>
    [HttpPost("{customerId:long}/interactions")]
    [ProducesResponseType<InteractionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InteractionDto>> CreateInteraction(long customerId, InteractionRequest request, CancellationToken ct)
    {
        var created = await interactions.CreateAsync(customerId, request, ct);
        return CreatedAtAction(nameof(InteractionsController.Get), "Interactions", new { id = created.Id }, created);
    }
}
