namespace TicketClassifier.Api.Dtos.Input;

/// <summary>Uma linha do CSV de entrada, já normalizada pelo parser.</summary>
public class TicketCsvInput
{
    public string? ExternalId { get; set; }
    public string? Assunto { get; set; }
    public string Descricao { get; set; } = string.Empty;
}
