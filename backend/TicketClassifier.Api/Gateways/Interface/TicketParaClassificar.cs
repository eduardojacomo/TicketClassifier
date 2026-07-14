namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>Item de entrada para classificação em lote (o índice preserva a ordem).</summary>
public record TicketParaClassificar(int Indice, string? Assunto, string Descricao);
