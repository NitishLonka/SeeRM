using Microsoft.AspNetCore.Mvc;
using SeeRM.Api.Contracts;
using SeeRM.Api.Services;

namespace SeeRM.Api.Controllers;

/// <summary>Follow-up tasks. Create is under /api/customers/{customerId}/tasks.</summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController(TaskService tasks) : ControllerBase
{
    /// <summary>List tasks, ordered by due date.</summary>
    /// <param name="customerId">Only tasks for this customer.</param>
    /// <param name="status">open | completed</param>
    /// <param name="dueBefore">Only tasks due on or before this date (yyyy-MM-dd).</param>
    /// <param name="ct"></param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<IReadOnlyList<TaskDto>> List(
        [FromQuery] long? customerId, [FromQuery] string? status, [FromQuery] DateOnly? dueBefore, CancellationToken ct) =>
        tasks.ListAsync(customerId, status, dueBefore, ct);

    /// <summary>Get one task.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TaskDto> Get(long id, CancellationToken ct) => tasks.GetAsync(id, ct);

    /// <summary>Update a task. Completing sets completedAt; reopening clears it.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TaskDto> Update(long id, TaskRequest request, CancellationToken ct) => tasks.UpdateAsync(id, request, ct);

    /// <summary>Delete a task.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await tasks.DeleteAsync(id, ct);
        return NoContent();
    }
}
