namespace TicketClassifier.Api.Models;

/// <summary>
/// Parâmetro configurável para classificação de tickets.
/// Tipos: categoria, prioridade, pergunta, reclamacao, sentimento_positivo, sentimento_negativo, tag, departamento
/// </summary>
public class ParametroClassificacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Tipo { get; set; } = string.Empty;
    public string Termo { get; set; } = string.Empty;
    public string? Alvo { get; set; }
    public bool Ativo { get; set; } = true;
}
