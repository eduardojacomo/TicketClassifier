namespace TicketClassifier.Api.Models;

/// <summary>Um ticket individual, com a classificação da IA.</summary>
public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }

    public string? ExternalId { get; set; }
    public string? Assunto { get; set; }
    public string Descricao { get; set; } = string.Empty;

    // Resultado da classificação
    public string Categoria { get; set; } = "Other";
    public string Prioridade { get; set; } = "Medium";
    public string Departamento { get; set; } = "Support";
    public string Resumo { get; set; } = string.Empty;
    public double Confianca { get; set; }
    public string Justificativa { get; set; } = string.Empty;
    public string Sentimento { get; set; } = "neutral";
    public string Tags { get; set; } = "[]";

    /// <summary>False quando a classificação falhou (caiu no fallback) — permite reprocessar.</summary>
    public bool ProcessadoOk { get; set; } = true;

    public bool RegistroModificado { get; set; }
    public DateTime? DataModificacao { get; set; }
}
