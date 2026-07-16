namespace TicketClassifier.Api.Models;

/// <summary>An individual ticket, with the AI classification.</summary>
public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }

    public string? ExternalId { get; set; }
    public string? Subject { get; set; }
    public string Description { get; set; } = string.Empty;

    // Classification result
    public string Category { get; set; } = "Other";
    public string Priority { get; set; } = "Medium";
    public string Department { get; set; } = "Support";
    public string Summary { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string Sentiment { get; set; } = "neutral";
    public string Tags { get; set; } = "[]";

    /// <summary>False when classification failed (fell back to default) — allows reprocessing.</summary>
    public bool ProcessedOk { get; set; } = true;

    public bool RecordModified { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
