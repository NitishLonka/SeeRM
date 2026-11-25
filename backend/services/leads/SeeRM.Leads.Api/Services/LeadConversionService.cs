using Microsoft.EntityFrameworkCore;
using SeeRM.Leads.Contracts;
using SeeRM.Leads.Data;
using SeeRM.Leads.Domain;
using SeeRM.Leads.Infrastructure;
using SeeRM.Leads.Integration;

namespace SeeRM.Leads.Services;

/// <summary>
/// Converts a Qualified lead into a customer in the Customer service (sync REST).
/// There is no distributed transaction, so every step is idempotent and the lead is only marked
/// Converted after the customer exists. Retrying after any failure is safe:
///   1. customer: looked up by email first, created only if missing (409 on create → look up again)
///   2. deal:     identified by a deterministic title ("… (lead #id)"), created only if not already there
///   3. lead:     marked Converted last
/// </summary>
public class LeadConversionService(LeadsDbContext db, CustomerServiceClient customers, LeadService leads,
    ILogger<LeadConversionService> logger)
{
    public async Task<ConvertLeadResultDto> ConvertAsync(long id, CancellationToken ct)
    {
        var lead = await db.Leads.FindAsync([id], ct) ?? throw new NotFoundException("Lead", id);

        if (lead.Status == LeadStatuses.Converted)
            throw new ConflictException($"Lead {id} was already converted to customer {lead.ConvertedCustomerId}.");
        if (lead.Status != LeadStatuses.Qualified)
            throw new BadRequestException($"Only Qualified leads can be converted (lead {id} is {lead.Status}).");
        if (string.IsNullOrWhiteSpace(lead.Phone))
            throw new BadRequestException($"Lead {id} needs a phone number before it can be converted (the Customer service requires one).");

        var warnings = new List<string>();

        // 1. Link to an existing customer with the same email, or create one.
        long customerId;
        bool customerCreated;
        var existing = await customers.FindByEmailAsync(lead.Email, ct);
        if (existing is not null)
        {
            (customerId, customerCreated) = (existing.Id, false);
        }
        else
        {
            var fullName = $"{lead.FirstName} {lead.LastName}";
            (customerId, customerCreated) = await customers.CreateOrGetCustomerAsync(new CustomerCreateRequest(
                FullName: fullName,
                Email: lead.Email,
                Phone: lead.Phone,
                AccountType: lead.CompanyName is null ? "individual" : "business",
                Company: lead.CompanyName,
                Status: "Active"), ct);
        }

        // 2. Carry the estimated value over as an open deal. Failure here doesn't block conversion.
        long? dealId = null;
        if (lead.EstimatedValue is > 0)
        {
            var title = DealTitle(lead);
            try
            {
                var deals = await customers.ListDealsAsync(customerId, ct);
                var deal = deals.FirstOrDefault(d => d.Title == title)
                           ?? await customers.CreateDealAsync(customerId, new DealCreateRequest(title, lead.EstimatedValue.Value, "qualification"), ct);
                dealId = deal.Id;
            }
            catch (ApiException ex)
            {
                logger.LogWarning(ex, "Deal creation failed for lead {LeadId} / customer {CustomerId}", id, customerId);
                warnings.Add($"Customer was linked, but the deal could not be created: {ex.Message}");
            }
        }

        // 3. Mark the lead Converted only after the customer exists.
        lead.Status = LeadStatuses.Converted;
        lead.ConvertedCustomerId = customerId;
        lead.ConvertedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new ConvertLeadResultDto(await leads.GetAsync(id, ct), customerId, customerCreated, dealId, warnings);
    }

    /// <summary>Deterministic, so a retried conversion finds the deal it already created.</summary>
    private static string DealTitle(Lead l)
    {
        var name = l.CompanyName ?? $"{l.FirstName} {l.LastName}";
        var title = $"{name} (lead #{l.Id})";
        return title.Length <= 200 ? title : title[..200];
    }
}
