using System.ComponentModel.DataAnnotations;

namespace HelpdeskApi.Models;

public static class TicketStatus
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Closed = "Closed";

    public static readonly string[] All = { Open, InProgress, Resolved, Closed };
}

// ---------- Read models (what the API returns) ----------

public class TicketSummary
{
    public int TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CustomerName { get; set; }
    public string? AgentName { get; set; }
    public string? Category { get; set; }
}

public class TicketDetail : TicketSummary
{
    public string Description { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    public List<TicketComment> Comments { get; set; } = new();
}

public class TicketComment
{
    public int CommentId { get; set; }
    public int TicketId { get; set; }
    public int? AgentId { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class TicketStat
{
    public string Status { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public double? AvgResolutionHours { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Company { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ---------- Request models (what the API accepts) ----------

public class CreateTicketRequest
{
    [Required] public int CustomerId { get; set; }
    [Required] public int CategoryId { get; set; }

    [Required, StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [AllowedValues("Low", "Medium", "High", "Critical")]
    public string Priority { get; set; } = "Medium";
}

public class AssignTicketRequest
{
    [Required] public int AgentId { get; set; }
}

public class UpdateStatusRequest
{
    [Required, AllowedValues("Open", "InProgress", "Resolved", "Closed")]
    public string Status { get; set; } = string.Empty;
}

public class AddCommentRequest
{
    /// <summary>Null means the comment was written by the customer.</summary>
    public int? AgentId { get; set; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;
}

public class CreateCustomerRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [StringLength(100)] public string? Company { get; set; }
}
