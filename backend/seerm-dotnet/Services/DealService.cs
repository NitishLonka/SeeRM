using Microsoft.EntityFrameworkCore;
using SeeRM.Api.Contracts;
using SeeRM.Api.Data;
using SeeRM.Api.Domain;
using SeeRM.Api.Infrastructure;

namespace SeeRM.Api.Services;

public class DealService(SeeRmDbContext db)
{
    public async Task<IReadOnlyList<DealDto>> ListForCustomerAsync(long customerId, CancellationToken ct)
    {
        await EnsureCustomerExists(customerId, ct);
        return await db.Deals.AsNoTracking()
            .Where(d => d.CustomerId == customerId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(ToDto)
            .ToListAsync(ct);
    }

    public async Task<DealDto> GetAsync(long id, CancellationToken ct) =>
        await db.Deals.AsNoTracking().Where(d => d.Id == id).Select(ToDto).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Deal", id);

    public async Task<DealDto> CreateAsync(long customerId, DealRequest req, CancellationToken ct)
    {
        await EnsureCustomerExists(customerId, ct);
        var deal = new Deal { CustomerId = customerId };
        Apply(deal, req);
        db.Deals.Add(deal);
        await db.SaveChangesAsync(ct);
        return await GetAsync(deal.Id, ct);
    }

    public async Task<DealDto> UpdateAsync(long id, DealRequest req, CancellationToken ct)
    {
        var deal = await db.Deals.FindAsync([id], ct) ?? throw new NotFoundException("Deal", id);
        Apply(deal, req);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Linked tasks are kept; the database sets their deal_id to null.</summary>
    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var deleted = await db.Deals.Where(d => d.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw new NotFoundException("Deal", id);
    }

    private static void Apply(Deal d, DealRequest req)
    {
        d.Title = req.Title.Trim();
        d.Value = req.Value;
        d.ExpectedCloseDate = req.ExpectedCloseDate;

        // closed_at is set exactly when the deal is won or lost (enforced by a DB CHECK).
        var isClosed = DealStages.Closed.Contains(req.Stage);
        var wasClosed = DealStages.Closed.Contains(d.Stage) && d.ClosedAt is not null;
        if (isClosed && (!wasClosed || d.Stage != req.Stage)) d.ClosedAt = DateTime.UtcNow;
        else if (!isClosed) d.ClosedAt = null;
        d.Stage = req.Stage;
    }

    private async Task EnsureCustomerExists(long customerId, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            throw new NotFoundException("Customer", customerId);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Deal, DealDto>> ToDto = d =>
        new DealDto(d.Id, d.CustomerId, d.Title, d.Value, d.Stage, d.ExpectedCloseDate, d.ClosedAt, d.CreatedAt, d.UpdatedAt);
}
