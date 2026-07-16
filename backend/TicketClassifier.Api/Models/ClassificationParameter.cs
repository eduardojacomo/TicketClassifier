namespace TicketClassifier.Api.Models;

/// <summary>
/// Configurable parameter for ticket classification.
/// Types: categoria, prioridade, pergunta, reclamacao, sentimento_positivo, sentimento_negativo, tag, departamento
/// </summary>
public class ClassificationParameter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public string? Target { get; set; }
    public bool Active { get; set; } = true;
}
