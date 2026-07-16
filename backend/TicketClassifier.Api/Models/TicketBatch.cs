namespace TicketClassifier.Api.Models;

/// <summary>A batch of tickets submitted via CSV.</summary>
public class TicketBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public int Total { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public List<Ticket> Tickets { get; set; } = new();
}
