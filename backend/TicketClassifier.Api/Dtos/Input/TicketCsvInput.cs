namespace TicketClassifier.Api.Dtos.Input;

/// <summary>A row from the input CSV, already normalized by the parser.</summary>
public class TicketCsvInput
{
    public string? ExternalId { get; set; }
    public string? Subject { get; set; }
    public string Description { get; set; } = string.Empty;
}
