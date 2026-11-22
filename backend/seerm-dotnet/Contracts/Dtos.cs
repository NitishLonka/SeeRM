using System.ComponentModel.DataAnnotations;
using SeeRM.Api.Domain;

namespace SeeRM.Api.Contracts;

// Request/response shapes for the API. Field names follow the React UI
// (fullName, company, accountType, ...). Validation messages match the UI form.

/// <summary>A page of results.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

// ---------------------------------------------------------------- Customers

/// <summary>Body for creating or updating a customer (the "New Customer" form).</summary>
public class CustomerRequest
{
    /// <example>Sarah Jenkins</example>
    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
    [MaxLength(150)]
    public string FullName { get; set; } = "";

    /// <example>sarah.jenkins@apexlogistics.example</example>
    [Required(ErrorMessage = "Email is required")]
    [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", ErrorMessage = "Please enter a valid email address")]
    [MaxLength(254)]
    public string Email { get; set; } = "";

    /// <example>555-201-1001</example>
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[0-9\-+\s()]{7,15}$", ErrorMessage = "Please enter a valid phone number")]
    public string Phone { get; set; } = "";

    /// <summary>individual | business</summary>
    /// <example>business</example>
    [AllowedValues(AccountTypes.Individual, AccountTypes.Business, ErrorMessage = "Account type must be 'individual' or 'business'")]
    public string AccountType { get; set; } = AccountTypes.Individual;

    /// <summary>Optional company name. Empty is stored as null.</summary>
    /// <example>Apex Logistics</example>
    [MaxLength(150)]
    public string? Company { get; set; }

    /// <summary>Lead | Active | Inactive. Not on the form; defaults to Lead on create, unchanged on update when omitted.</summary>
    /// <example>Lead</example>
    [AllowedValues(null, CustomerStatuses.Lead, CustomerStatuses.Active, CustomerStatuses.Inactive,
        ErrorMessage = "Status must be 'Lead', 'Active' or 'Inactive'")]
    public string? Status { get; set; }
}

/// <summary>Row in the dashboard's Customer Directory table.</summary>
public record CustomerListItemDto(
    long Id,
    string Name,
    string? Company,
    string Status,
    decimal DealValue,
    DateTime? LastContactAt);

/// <summary>Full customer record.</summary>
public record CustomerDto(
    long Id,
    string FullName,
    string Email,
    string Phone,
    string AccountType,
    string? Company,
    string Status,
    decimal DealValue,
    decimal PipelineValue,
    DateTime? LastContactAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ---------------------------------------------------------------- Deals

public class DealRequest
{
    /// <example>Warehouse analytics add-on</example>
    [Required, MinLength(2), MaxLength(200)]
    public string Title { get; set; } = "";

    /// <example>5000</example>
    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "Value must be between 0 and 9,999,999,999.99")]
    public decimal Value { get; set; }

    /// <summary>prospecting | qualification | proposal | negotiation | won | lost</summary>
    /// <example>proposal</example>
    [AllowedValues(DealStages.Prospecting, DealStages.Qualification, DealStages.Proposal,
        DealStages.Negotiation, DealStages.Won, DealStages.Lost)]
    public string Stage { get; set; } = DealStages.Prospecting;

    /// <example>2026-12-15</example>
    public DateOnly? ExpectedCloseDate { get; set; }
}

public record DealDto(
    long Id,
    long CustomerId,
    string Title,
    decimal Value,
    string Stage,
    DateOnly? ExpectedCloseDate,
    DateTime? ClosedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ---------------------------------------------------------------- Tasks

public class TaskRequest
{
    /// <example>Follow up on pricing questions</example>
    [Required, MinLength(2), MaxLength(200)]
    public string Title { get; set; } = "";

    /// <summary>Optional deal this task belongs to; must belong to the same customer.</summary>
    public long? DealId { get; set; }

    /// <example>2026-11-25</example>
    public DateOnly? DueDate { get; set; }

    /// <summary>open | completed</summary>
    /// <example>open</example>
    [AllowedValues(TaskStatuses.Open, TaskStatuses.Completed)]
    public string Status { get; set; } = TaskStatuses.Open;
}

public record TaskDto(
    long Id,
    long CustomerId,
    long? DealId,
    string Title,
    DateOnly? DueDate,
    string Status,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ---------------------------------------------------------------- Interactions

public class InteractionRequest
{
    /// <summary>call | email | meeting | note</summary>
    /// <example>call</example>
    [AllowedValues(InteractionTypes.Call, InteractionTypes.Email, InteractionTypes.Meeting, InteractionTypes.Note)]
    public string Type { get; set; } = InteractionTypes.Note;

    /// <example>Discussed renewal timeline</example>
    [Required, MinLength(1), MaxLength(500)]
    public string Summary { get; set; } = "";

    /// <summary>When it happened (UTC). Defaults to now.</summary>
    public DateTime? OccurredAt { get; set; }
}

public record InteractionDto(
    long Id,
    long CustomerId,
    string Type,
    string Summary,
    DateTime OccurredAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

// ---------------------------------------------------------------- Dashboard

/// <summary>Values for the dashboard KPI cards.</summary>
public record DashboardSummaryDto(
    int TotalCustomers,
    int ActiveLeads,
    decimal PipelineRevenue,
    int TasksCompleted,
    int TasksTotal,
    int FollowUpsDue);
