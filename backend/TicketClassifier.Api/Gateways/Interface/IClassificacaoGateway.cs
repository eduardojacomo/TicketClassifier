using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>
/// Classification strategy. Works in BATCHES — several tickets per call —
/// to drastically reduce the number of requests to the provider.
/// </summary>
public interface IClassificationGateway
{
    string Name { get; }

    /// <summary>Classifies a batch; returns the results in the SAME order as the input.</summary>
    Task<IReadOnlyList<ClassificationResult>> ClassifyBatchAsync(
        IReadOnlyList<TicketToClassify> items, ClassificationPromptBuilder promptBuilder,
        CancellationToken ct = default,
        int currentBatch = 1, int totalBatches = 1, int totalTickets = 0);
}
