namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>Input item for batch classification (the index preserves order).</summary>
public record TicketToClassify(int Index, string? Subject, string Description);
