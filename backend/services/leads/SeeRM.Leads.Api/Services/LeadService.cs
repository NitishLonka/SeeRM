using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SeeRM.Leads.Contracts;
using SeeRM.Leads.Data;
using SeeRM.Leads.Domain;
using SeeRM.Leads.Infrastructure;

namespace SeeRM.Leads.Services;

public class LeadService(LeadsDbContext db)
{
    public const int NewThisWeekDays = 7;

    /// <summary>Leads list, newest first, with case-insensitive search on name, company or email.</summary>
    public async Task<PagedResult<LeadDto>> ListAsync(
        string? search, string? status, string? source, int page, int pageSize, CancellationToken ct)
    {
        if (status is not null && !LeadStatuses.All.Contains(status))
            throw new BadRequestException($"Unknown status '{status}'. Allowed: {string.Join(", ", LeadStatuses.All)}.");
        if (source is not null && !LeadSources.All.Contains(source))
            throw new BadRequestException($"Unknown source '{source}'. Allowed: {string.Join(", ", LeadSources.All)}.");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.Leads.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            q = q.Where(l => EF.Functions.ILike(l.FirstName + " " + l.LastName, pattern)
                          || EF.Functions.ILike(l.Email, pattern)
                          || (l.CompanyName != null && EF.Functions.ILike(l.CompanyName, pattern)));
        }
        if (status is not null) q = q.Where(l => l.Status == status);
        if (source is not null) q = q.Where(l => l.Source == source);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(ct);

        return new PagedResult<LeadDto>(items, page, pageSize, total);
    }

    public async Task<LeadDto> GetAsync(long id, CancellationToken ct) =>
        await db.Leads.AsNoTracking().Where(l => l.Id == id).Select(ToDto).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Lead", id);

    public async Task<LeadDto> CreateAsync(LeadRequest req, CancellationToken ct)
    {
        var lead = new Lead();
        await ApplyAsync(lead, req, ct);
        db.Leads.Add(lead);
        await db.SaveChangesAsync(ct);
        return await GetAsync(lead.Id, ct);
    }

    public async Task<LeadDto> UpdateAsync(long id, LeadRequest req, CancellationToken ct)
    {
        var lead = await db.Leads.FindAsync([id], ct) ?? throw new NotFoundException("Lead", id);
        if (lead.Status == LeadStatuses.Converted)
            throw new ConflictException($"Lead {id} has been converted to customer {lead.ConvertedCustomerId} and can no longer be edited.");

        await ApplyAsync(lead, req, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var deleted = await db.Leads.Where(l => l.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw new NotFoundException("Lead", id);
    }

    public async Task<LeadSummaryDto> GetSummaryAsync(CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-NewThisWeekDays);

        var total = await db.Leads.CountAsync(ct);
        var newThisWeek = await db.Leads.CountAsync(l => l.CreatedAt >= since, ct);
        var qualified = await db.Leads.CountAsync(l => l.Status == LeadStatuses.Qualified, ct);
        var converted = await db.Leads.CountAsync(l => l.Status == LeadStatuses.Converted, ct);
        var openValue = await db.Leads
            .Where(l => LeadStatuses.Open.Contains(l.Status))
            .SumAsync(l => l.EstimatedValue ?? 0, ct);
        var rate = total == 0 ? 0 : Math.Round(converted * 100m / total, 1);

        return new LeadSummaryDto(total, newThisWeek, qualified, converted, rate, openValue);
    }

    private async Task ApplyAsync(Lead l, LeadRequest req, CancellationToken ct)
    {
        var firstName = req.FirstName.Trim();
        var lastName = req.LastName.Trim();
        var email = req.Email.Trim();
        if (firstName.Length == 0) throw new BadRequestException("First name is required");
        if (lastName.Length == 0) throw new BadRequestException("Last name is required");

        var emailLower = email.ToLower();
        if (await db.Leads.AnyAsync(x => x.Email.ToLower() == emailLower && x.Id != l.Id, ct))
            throw new ConflictException($"A lead with email '{email}' already exists.");

        l.FirstName = firstName;
        l.LastName = lastName;
        l.Email = email;
        l.Phone = NullIfBlank(req.Phone);
        l.CompanyName = NullIfBlank(req.Company);
        l.JobTitle = NullIfBlank(req.JobTitle);
        l.Source = req.Source;
        l.Status = req.Status;
        l.Score = req.Score;
        l.EstimatedValue = req.EstimatedValue;
        l.Notes = NullIfBlank(req.Notes);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string EscapeLike(string s) =>
        s.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    private static readonly Expression<Func<Lead, LeadDto>> ToDto = l => new LeadDto(
        l.Id, l.FirstName, l.LastName, l.FirstName + " " + l.LastName, l.Email, l.Phone, l.CompanyName, l.JobTitle,
        l.Source, l.Status, l.Score, l.EstimatedValue, l.Notes, l.ConvertedCustomerId, l.ConvertedAt,
        l.CreatedAt, l.UpdatedAt);
}
