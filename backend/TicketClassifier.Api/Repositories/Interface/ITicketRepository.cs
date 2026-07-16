using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Repositories.Interface;

public interface ITicketRepository
{
    Task<TicketBatch> AddAsync(TicketBatch batch, CancellationToken ct = default);
    Task<TicketBatch?> GetBatchAsync(Guid id, CancellationToken ct = default);
    Task<List<TicketBatch>> ListBatchesAsync(CancellationToken ct = default);
    Task<List<Ticket>> GetTicketsAsync(Guid batchId, CancellationToken ct = default);
    Task<List<Ticket>> GetFailedTicketsAsync(Guid batchId, CancellationToken ct = default);
    Task UpdateTicketsAsync(IEnumerable<Ticket> tickets, CancellationToken ct = default);
    Task<bool> BatchExistsAsync(Guid id, CancellationToken ct = default);
    Task<TicketBatch?> GetBatchByNameAsync(string fileName, CancellationToken ct = default);
    Task RemoveBatchAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> GetTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task UpdateTicketAsync(Ticket ticket, CancellationToken ct = default);

    // Similarities
    Task AddSimilaritiesAsync(IEnumerable<TicketSimilarity> similarities, CancellationToken ct = default);
    Task RemoveSimilaritiesByBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<List<TicketSimilarity>> GetSimilaritiesAsync(Guid ticketId, CancellationToken ct = default);
    Task RemoveSimilarityAsync(Guid id, CancellationToken ct = default);
    Task<int> CountSimilarAsync(Guid ticketId, CancellationToken ct = default);

    // Classification parameters
    Task<List<ClassificationParameter>> ListParametersAsync(string? type = null, CancellationToken ct = default);
    Task<ClassificationParameter?> GetParameterAsync(Guid id, CancellationToken ct = default);
    Task AddParameterAsync(ClassificationParameter parameter, CancellationToken ct = default);
    Task UpdateParameterAsync(ClassificationParameter parameter, CancellationToken ct = default);
    Task RemoveParameterAsync(Guid id, CancellationToken ct = default);
    Task<int> CountParametersAsync(CancellationToken ct = default);
    Task AddParametersAsync(IEnumerable<ClassificationParameter> parameters, CancellationToken ct = default);
}
