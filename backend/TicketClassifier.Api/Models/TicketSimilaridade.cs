namespace TicketClassifier.Api.Models;

public class TicketSimilarity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceTicketId { get; set; }
    public Guid RelatedTicketId { get; set; }
    public string SharedTags { get; set; } = "[]";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
