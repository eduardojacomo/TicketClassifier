namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>Result of classifying a ticket.</summary>
public record ClassificationResult(
    string Category,
    string Priority,
    string Department,
    string Summary,
    double Confidence,
    string Justification,
    string Sentiment,
    string[] Tags);
