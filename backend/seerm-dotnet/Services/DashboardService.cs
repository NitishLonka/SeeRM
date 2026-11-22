using Microsoft.EntityFrameworkCore;
using SeeRM.Api.Contracts;
using SeeRM.Api.Data;
using SeeRM.Api.Domain;

namespace SeeRM.Api.Services;

public class DashboardService(SeeRmDbContext db)
{
    /// <summary>Number of days ahead an open task counts as a "follow-up due".</summary>
    public const int FollowUpWindowDays = 7;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var dueBy = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(FollowUpWindowDays);

        var totalCustomers = await db.Customers.CountAsync(ct);
        var activeLeads = await db.Customers.CountAsync(c => c.Status == CustomerStatuses.Lead, ct);
        var pipelineRevenue = await db.Deals
            .Where(d => d.Stage != DealStages.Won && d.Stage != DealStages.Lost)
            .SumAsync(d => (decimal?)d.Value, ct) ?? 0;
        var tasksTotal = await db.Tasks.CountAsync(ct);
        var tasksCompleted = await db.Tasks.CountAsync(t => t.Status == TaskStatuses.Completed, ct);
        var followUpsDue = await db.Tasks.CountAsync(
            t => t.Status == TaskStatuses.Open && t.DueDate != null && t.DueDate <= dueBy, ct);

        return new DashboardSummaryDto(totalCustomers, activeLeads, pipelineRevenue, tasksCompleted, tasksTotal, followUpsDue);
    }
}
