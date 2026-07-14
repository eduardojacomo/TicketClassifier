namespace TicketClassifier.Api.Models;

/// <summary>Um lote de tickets enviado via CSV.</summary>
public class TicketBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeArquivo { get; set; } = string.Empty;
    public int Total { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public List<Ticket> Tickets { get; set; } = new();
}
