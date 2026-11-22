using Microsoft.EntityFrameworkCore;
using SeeRM.Api.Contracts;
using SeeRM.Api.Data;
using SeeRM.Api.Domain;
using SeeRM.Api.Infrastructure;

namespace SeeRM.Api.Services;

public class CustomerService(SeeRmDbContext db)
{
    /// <summary>Directory list with case-insensitive search on name or company.</summary>
    public async Task<PagedResult<CustomerListItemDto>> ListAsync(
        string? search, string? status, int page, int pageSize, CancellationToken ct)
    {
        if (status is not null && !CustomerStatuses.All.Contains(status))
            throw new BadRequestException($"Unknown status '{status}'. Allowed: {string.Join(", ", CustomerStatuses.All)}.");

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(c => EF.Functions.ILike(c.FullName, pattern)
                                  || (c.CompanyName != null && EF.Functions.ILike(c.CompanyName, pattern)));
        }
        if (status is not null)
            query = query.Where(c => c.Status == status);

        var total = await query.CountAsync(ct);

        var items = await query
            // Most recently contacted first; never-contacted customers last.
            // (Ordering must happen on the entity; EF can't sort on a constructor-projected DTO.)
            .OrderBy(c => !c.Interactions.Any())
            .ThenByDescending(c => c.Interactions.Max(i => (DateTime?)i.OccurredAt))
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItemDto(
                c.Id,
                c.FullName,
                c.CompanyName,
                c.Status,
                c.Deals.Where(d => d.Stage != DealStages.Lost).Sum(d => (decimal?)d.Value) ?? 0,
                c.Interactions.Max(i => (DateTime?)i.OccurredAt)))
            .ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>(items, page, pageSize, total);
    }

    public async Task<CustomerDto> GetAsync(long id, CancellationToken ct) =>
        await ProjectDetail(db.Customers.AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Customer", id);

    public async Task<CustomerDto> CreateAsync(CustomerRequest req, CancellationToken ct)
    {
        var customer = new Customer();
        await ApplyAsync(customer, req, isNew: true, ct);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return await GetAsync(customer.Id, ct);
    }

    public async Task<CustomerDto> UpdateAsync(long id, CustomerRequest req, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct) ?? throw new NotFoundException("Customer", id);
        await ApplyAsync(customer, req, isNew: false, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>Hard delete; the database cascades to deals, tasks and interactions.</summary>
    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var deleted = await db.Customers.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0) throw new NotFoundException("Customer", id);
    }

    private async Task ApplyAsync(Customer c, CustomerRequest req, bool isNew, CancellationToken ct)
    {
        var fullName = req.FullName.Trim();
        var email = req.Email.Trim();
        if (fullName.Length < 2)
            throw new BadRequestException("Name must be at least 2 characters");

        var emailLower = email.ToLower();
        var duplicate = await db.Customers.AnyAsync(x => x.Email.ToLower() == emailLower && x.Id != c.Id, ct);
        if (duplicate)
            throw new ConflictException($"A customer with email '{email}' already exists.");

        c.FullName = fullName;
        c.Email = email;
        c.Phone = req.Phone.Trim();
        c.AccountType = req.AccountType;
        c.CompanyName = string.IsNullOrWhiteSpace(req.Company) ? null : req.Company.Trim();
        if (req.Status is not null) c.Status = req.Status;
        else if (isNew) c.Status = CustomerStatuses.Lead;
    }

    private static IQueryable<CustomerDto> ProjectDetail(IQueryable<Customer> q) =>
        q.Select(c => new CustomerDto(
            c.Id,
            c.FullName,
            c.Email,
            c.Phone,
            c.AccountType,
            c.CompanyName,
            c.Status,
            c.Deals.Where(d => d.Stage != DealStages.Lost).Sum(d => (decimal?)d.Value) ?? 0,
            c.Deals.Where(d => d.Stage != DealStages.Won && d.Stage != DealStages.Lost).Sum(d => (decimal?)d.Value) ?? 0,
            c.Interactions.Max(i => (DateTime?)i.OccurredAt),
            c.CreatedAt,
            c.UpdatedAt));

    /// <summary>Escapes LIKE wildcards so user input is matched literally (Postgres default escape is '\').</summary>
    private static string EscapeLike(string s) =>
        s.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
