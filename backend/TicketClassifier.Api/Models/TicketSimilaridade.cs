namespace TicketClassifier.Api.Models;

public class TicketSimilaridade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketOrigemId { get; set; }
    public Guid TicketRelacionadoId { get; set; }
    public string TagsCompartilhadas { get; set; } = "[]";
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}
