namespace SeeRM.Api.Domain;

// EF Core entities mapped to the tables created by sqlscripts/001_create_tables.sql.
// Enum-like columns are kept as strings so the values match the DB CHECK constraints
// and the UI exactly (e.g. "Lead", "individual", "won"). Allowed values live in DomainValues.

public class Customer
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string AccountType { get; set; } = AccountTypes.Individual;
    public string? CompanyName { get; set; }
    public string Status { get; set; } = CustomerStatuses.Lead;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Deal> Deals { get; set; } = [];
    public List<TaskItem> Tasks { get; set; } = [];
    public List<Interaction> Interactions { get; set; } = [];
}

public class Deal
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Title { get; set; } = "";
    public decimal Value { get; set; }
    public string Stage { get; set; } = DealStages.Prospecting;
    public DateOnly? ExpectedCloseDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? Customer { get; set; }
}

/// <summary>Row in the <c>tasks</c> table (named TaskItem to avoid clashing with System.Threading.Tasks.Task).</summary>
public class TaskItem
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long? DealId { get; set; }
    public string Title { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = TaskStatuses.Open;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? Customer { get; set; }
    public Deal? Deal { get; set; }
}

public class Interaction
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string Type { get; set; } = InteractionTypes.Note;
    public string Summary { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Customer? Customer { get; set; }
}
