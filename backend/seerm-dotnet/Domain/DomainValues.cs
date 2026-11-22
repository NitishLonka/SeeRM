namespace SeeRM.Api.Domain;

// Allowed values for string-backed columns. Must stay in sync with the CHECK constraints
// in sqlscripts/001_create_tables.sql.

public static class AccountTypes
{
    public const string Individual = "individual";
    public const string Business = "business";
    public static readonly string[] All = [Individual, Business];
}

public static class CustomerStatuses
{
    public const string Lead = "Lead";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public static readonly string[] All = [Lead, Active, Inactive];
}

public static class DealStages
{
    public const string Prospecting = "prospecting";
    public const string Qualification = "qualification";
    public const string Proposal = "proposal";
    public const string Negotiation = "negotiation";
    public const string Won = "won";
    public const string Lost = "lost";
    public static readonly string[] All = [Prospecting, Qualification, Proposal, Negotiation, Won, Lost];
    public static readonly string[] Closed = [Won, Lost];
}

public static class TaskStatuses
{
    public const string Open = "open";
    public const string Completed = "completed";
    public static readonly string[] All = [Open, Completed];
}

public static class InteractionTypes
{
    public const string Call = "call";
    public const string Email = "email";
    public const string Meeting = "meeting";
    public const string Note = "note";
    public static readonly string[] All = [Call, Email, Meeting, Note];
}
