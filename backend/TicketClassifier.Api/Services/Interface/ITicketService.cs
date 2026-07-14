using TicketClassifier.Api.Dtos.Output;

namespace TicketClassifier.Api.Services.Interface;

public interface ITicketService
{
    Task<BatchResumoDto> ProcessarCsvAsync(Stream csv, string nomeArquivo, Guid jobId, CancellationToken ct = default);
    Task<BatchDetalheDto?> ReprocessarFalhasAsync(Guid batchId, CancellationToken ct = default);
    Task<List<BatchResumoDto>> ListarAsync(CancellationToken ct = default);
    Task<BatchDetalheDto?> ObterAsync(Guid id, CancellationToken ct = default);
    Task<byte[]?> ExportarAsync(Guid id, CancellationToken ct = default);
}
