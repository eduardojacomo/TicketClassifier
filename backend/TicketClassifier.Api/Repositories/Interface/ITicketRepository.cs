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
    Task<TicketBatch?> ObterBatchPorNomeAsync(string nomeArquivo, CancellationToken ct = default);
    Task RemoverBatchAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> ObterTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task AtualizarTicketAsync(Ticket ticket, CancellationToken ct = default);

    // Similaridades
    Task AdicionarSimilaridadesAsync(IEnumerable<TicketSimilaridade> similaridades, CancellationToken ct = default);
    Task RemoverSimilaridadesPorBatchAsync(Guid batchId, CancellationToken ct = default);
    Task<List<TicketSimilaridade>> ObterSimilaridadesAsync(Guid ticketId, CancellationToken ct = default);
    Task RemoverSimilaridadeAsync(Guid id, CancellationToken ct = default);
    Task<int> ContarSimilaresAsync(Guid ticketId, CancellationToken ct = default);

    // Parâmetros de classificação
    Task<List<ParametroClassificacao>> ListarParametrosAsync(string? tipo = null, CancellationToken ct = default);
    Task<ParametroClassificacao?> ObterParametroAsync(Guid id, CancellationToken ct = default);
    Task AdicionarParametroAsync(ParametroClassificacao parametro, CancellationToken ct = default);
    Task AtualizarParametroAsync(ParametroClassificacao parametro, CancellationToken ct = default);
    Task RemoverParametroAsync(Guid id, CancellationToken ct = default);
    Task<int> ContarParametrosAsync(CancellationToken ct = default);
    Task AdicionarParametrosAsync(IEnumerable<ParametroClassificacao> parametros, CancellationToken ct = default);
}
