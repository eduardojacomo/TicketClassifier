namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>Resultado da classificação de um ticket.</summary>
public record ClassificacaoResultado(
    string Categoria,
    string Prioridade,
    string Departamento,
    string Resumo,
    double Confianca,
    string Justificativa);
