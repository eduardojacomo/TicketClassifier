using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Repositories.Interface;

public interface ITicketRepository
{
    Task<TicketBatch> AdicionarAsync(TicketBatch batch, CancellationToken ct = default);
    Task<TicketBatch?> ObterBatchAsync(Guid id, CancellationToken ct = default);
    Task<List<TicketBatch>> ListarBatchesAsync(CancellationToken ct = default);
    Task<List<Ticket>> ObterTicketsAsync(Guid batchId, CancellationToken ct = default);
    Task<List<Ticket>> ObterTicketsComFalhaAsync(Guid batchId, CancellationToken ct = default);
    Task AtualizarTicketsAsync(IEnumerable<Ticket> tickets, CancellationToken ct = default);
    Task<bool> BatchExisteAsync(Guid id, CancellationToken ct = default);
}
