namespace SeeRM.Leads.Domain;

/// <summary>Row in the <c>leads</c> table (sqlscripts/leads/001_create_tables.sql).</summary>
public class Lead
{
    public long Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public string? JobTitle { get; set; }
    public string Source { get; set; } = LeadSources.Website;
    public string Status { get; set; } = LeadStatuses.New;
    public short Score { get; set; }
    public decimal? EstimatedValue { get; set; }
    public string? Notes { get; set; }

    /// <summary>customers.id in the Customer service database. Not a foreign key (different service).</summary>
    public long? ConvertedCustomerId { get; set; }
    public DateTime? ConvertedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Allowed values; must match the CHECK constraints in sqlscripts/leads/001_create_tables.sql.

public static class LeadStatuses
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string Qualified = "Qualified";
    public const string Unqualified = "Unqualified";
    public const string Converted = "Converted";
    public static readonly string[] All = [New, Contacted, Qualified, Unqualified, Converted];
    /// <summary>Statuses that still count towards the open pipeline.</summary>
    public static readonly string[] Open = [New, Contacted, Qualified];
}

public static class LeadSources
{
    public const string Website = "website";
    public const string Referral = "referral";
    public const string Event = "event";
    public const string ColdCall = "cold_call";
    public const string Social = "social";
    public const string Other = "other";
    public static readonly string[] All = [Website, Referral, Event, ColdCall, Social, Other];
}
