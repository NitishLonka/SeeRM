using Microsoft.EntityFrameworkCore;
using SeeRM.Api.Contracts;
using SeeRM.Api.Data;
using SeeRM.Api.Domain;
using SeeRM.Api.Infrastructure;

namespace SeeRM.Api.Services;

public class InteractionService(SeeRmDbContext db)
{
    public async Task<IReadOnlyList<InteractionDto>> ListForCustomerAsync(long customerId, CancellationToken ct)
    {
        await EnsureCustomerExists(customerId, ct);
        return await db.Interactions.AsNoTracking()
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.OccurredAt)
            .Select(ToDto)
            .ToListAsync(ct);
    }

    public async Task<InteractionDto> GetAsync(long id, CancellationToken ct) =>
        await db.Interactions.AsNoTracking().Where(i => i.Id == id).Select(ToDto).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Interaction", id);

    public async Task<InteractionDto> CreateAsync(long customerId, InteractionRequest req, CancellationToken ct)
    {
        await EnsureCustomerExists(customerId, ct);
        var interaction = new Interaction { CustomerId = customerId };
        Apply(interaction, req);
        db.Interactions.Add(interaction);
        await db.SaveChangesAsync(ct);
        return await GetAsync(interaction.Id, ct);
    }

    public async Task<InteractionDto> UpdateAsync(long id, InteractionRequest req, CancellationToken ct)
    {
        var interaction = await db.Interactions.FindAsync([id], ct) ?? throw new NotFoundException("Interaction", id);
        Apply(interaction, req);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var deleted = await db.Interactions.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw new NotFoundException("Interaction", id);
    }

    private static void Apply(Interaction i, InteractionRequest req)
    {
        i.Type = req.Type;
        i.Summary = req.Summary.Trim();
        i.OccurredAt = req.OccurredAt is { } at ? ToUtc(at) : DateTime.UtcNow;
    }

    // timestamptz columns require UTC DateTimes; treat unspecified input as UTC.
    private static DateTime ToUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
    };

    private async Task EnsureCustomerExists(long customerId, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == customerId, ct))
            throw new NotFoundException("Customer", customerId);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Interaction, InteractionDto>> ToDto = i =>
        new InteractionDto(i.Id, i.CustomerId, i.Type, i.Summary, i.OccurredAt, i.CreatedAt, i.UpdatedAt);
}
