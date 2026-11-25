using System.ComponentModel.DataAnnotations;
using SeeRM.Leads.Domain;

namespace SeeRM.Leads.Contracts;

/// <summary>A page of results.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Body for creating or updating a lead. Status "Converted" can only be reached through the convert endpoint.</summary>
public class LeadRequest
{
    /// <example>Olivia</example>
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(80)]
    public string FirstName { get; set; } = "";

    /// <example>Bennett</example>
    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(80)]
    public string LastName { get; set; } = "";

    /// <example>olivia.bennett@tidewaterfreight.example</example>
    [Required(ErrorMessage = "Email is required")]
    [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", ErrorMessage = "Please enter a valid email address")]
    [MaxLength(254)]
    public string Email { get; set; } = "";

    /// <summary>Optional, but required before the lead can be converted to a customer.</summary>
    /// <example>555-301-2001</example>
    [RegularExpression(@"^[0-9\-+\s()]{7,15}$", ErrorMessage = "Please enter a valid phone number")]
    public string? Phone { get; set; }

    /// <example>Tidewater Freight</example>
    [MaxLength(150)]
    public string? Company { get; set; }

    /// <example>Operations Manager</example>
    [MaxLength(120)]
    public string? JobTitle { get; set; }

    /// <summary>website | referral | event | cold_call | social | other</summary>
    /// <example>website</example>
    [AllowedValues(LeadSources.Website, LeadSources.Referral, LeadSources.Event,
        LeadSources.ColdCall, LeadSources.Social, LeadSources.Other,
        ErrorMessage = "Source must be one of: website, referral, event, cold_call, social, other")]
    public string Source { get; set; } = LeadSources.Website;

    /// <summary>New | Contacted | Qualified | Unqualified</summary>
    /// <example>New</example>
    [AllowedValues(LeadStatuses.New, LeadStatuses.Contacted, LeadStatuses.Qualified, LeadStatuses.Unqualified,
        ErrorMessage = "Status must be one of: New, Contacted, Qualified, Unqualified (use the convert endpoint for Converted)")]
    public string Status { get; set; } = LeadStatuses.New;

    /// <summary>0–100</summary>
    /// <example>35</example>
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
    public short Score { get; set; }

    /// <example>12000</example>
    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Estimated value must be between 0 and 9,999,999,999.99")]
    public decimal? EstimatedValue { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public record LeadDto(
    long Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    string? Company,
    string? JobTitle,
    string Source,
    string Status,
    short Score,
    decimal? EstimatedValue,
    string? Notes,
    long? ConvertedCustomerId,
    DateTime? ConvertedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Result of converting a lead into a customer.</summary>
/// <param name="Lead">The lead, now Converted.</param>
/// <param name="CustomerId">Customer id in the Customer service.</param>
/// <param name="CustomerCreated">True if a new customer was created; false if the lead was linked to an existing customer with the same email.</param>
/// <param name="DealId">Deal created (or found) for the lead's estimated value, if any.</param>
/// <param name="Warnings">Non-fatal problems, e.g. the deal couldn't be created.</param>
public record ConvertLeadResultDto(
    LeadDto Lead,
    long CustomerId,
    bool CustomerCreated,
    long? DealId,
    IReadOnlyList<string> Warnings);

/// <summary>Values for the Leads screen KPI cards.</summary>
/// <param name="TotalLeads">All leads.</param>
/// <param name="NewThisWeek">Leads created in the last 7 days.</param>
/// <param name="Qualified">Leads currently Qualified.</param>
/// <param name="Converted">Leads converted to customers.</param>
/// <param name="ConversionRate">Converted / total leads, as a percentage (one decimal).</param>
/// <param name="OpenPipelineValue">Sum of estimated value for New, Contacted and Qualified leads.</param>
public record LeadSummaryDto(
    int TotalLeads,
    int NewThisWeek,
    int Qualified,
    int Converted,
    decimal ConversionRate,
    decimal OpenPipelineValue);
