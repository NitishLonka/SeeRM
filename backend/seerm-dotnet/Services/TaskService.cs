using Microsoft.EntityFrameworkCore;
using SeeRM.Api.Contracts;
using SeeRM.Api.Data;
using SeeRM.Api.Domain;
using SeeRM.Api.Infrastructure;

namespace SeeRM.Api.Services;

public class TaskService(SeeRmDbContext db)
{
    /// <summary>Tasks filtered by customer, status and/or due date (inclusive).</summary>
    public async Task<IReadOnlyList<TaskDto>> ListAsync(
        long? customerId, string? status, DateOnly? dueBefore, CancellationToken ct)
    {
        if (status is not null && !TaskStatuses.All.Contains(status))
            throw new BadRequestException($"Unknown status '{status}'. Allowed: {string.Join(", ", TaskStatuses.All)}.");

        var q = db.Tasks.AsNoTracking();
        if (customerId is not null) q = q.Where(t => t.CustomerId == customerId);
        if (status is not null) q = q.Where(t => t.Status == status);
        if (dueBefore is not null) q = q.Where(t => t.DueDate != null && t.DueDate <= dueBefore);

        return await q
            .OrderBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .Select(ToDto)
            .ToListAsync(ct);
    }

    public async Task<TaskDto> GetAsync(long id, CancellationToken ct) =>
        await db.Tasks.AsNoTracking().Where(t => t.Id == id).Select(ToDto).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Task", id);

    public async Task<TaskDto> CreateAsync(long customerId, TaskRequest req, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            throw new NotFoundException("Customer", customerId);

        var task = new TaskItem { CustomerId = customerId };
        await ApplyAsync(task, req, ct);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(ct);
        return await GetAsync(task.Id, ct);
    }

    public async Task<TaskDto> UpdateAsync(long id, TaskRequest req, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync([id], ct) ?? throw new NotFoundException("Task", id);
        await ApplyAsync(task, req, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var deleted = await db.Tasks.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw new NotFoundException("Task", id);
    }

    private async Task ApplyAsync(TaskItem t, TaskRequest req, CancellationToken ct)
    {
        if (req.DealId is not null)
        {
            var dealCustomer = await db.Deals.Where(d => d.Id == req.DealId)
                .Select(d => (long?)d.CustomerId).FirstOrDefaultAsync(ct);
            if (dealCustomer is null) throw new BadRequestException($"Deal {req.DealId} does not exist.");
            if (dealCustomer != t.CustomerId)
                throw new BadRequestException($"Deal {req.DealId} belongs to a different customer.");
        }

        t.Title = req.Title.Trim();
        t.DealId = req.DealId;
        t.DueDate = req.DueDate;

        // completed_at is set exactly when the task is completed (enforced by a DB CHECK).
        if (req.Status == TaskStatuses.Completed && t.Status != TaskStatuses.Completed) t.CompletedAt = DateTime.UtcNow;
        else if (req.Status == TaskStatuses.Open) t.CompletedAt = null;
        t.Status = req.Status;
    }

    private static readonly System.Linq.Expressions.Expression<Func<TaskItem, TaskDto>> ToDto = t =>
        new TaskDto(t.Id, t.CustomerId, t.DealId, t.Title, t.DueDate, t.Status, t.CompletedAt, t.CreatedAt, t.UpdatedAt);
}
