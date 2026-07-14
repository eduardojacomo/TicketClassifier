using TicketClassifier.Api.Dtos.Output;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Services.Interface;

public interface ITicketService
{
    Task<BatchResumoDto?> VerificarDuplicataAsync(string nomeArquivo, CancellationToken ct = default);
    Task<BatchResumoDto> ProcessarCsvAsync(Stream csv, string nomeArquivo, Guid jobId, bool sobrescrever = false, CancellationToken ct = default);
    Task<BatchDetalheDto?> ReprocessarFalhasAsync(Guid batchId, CancellationToken ct = default);
    Task<BatchDetalheDto?> ReprocessarTudoAsync(Guid batchId, Guid jobId, CancellationToken ct = default);
    Task<List<BatchResumoDto>> ListarAsync(CancellationToken ct = default);
    Task<BatchDetalheDto?> ObterAsync(Guid id, CancellationToken ct = default);
    Task<byte[]?> ExportarAsync(Guid id, string[]? colunas = null, CancellationToken ct = default);
    Task<TicketDto?> AtualizarTicketAsync(Guid ticketId, TicketEditDto dto, CancellationToken ct = default);
    Task<List<TicketDto>> ObterSimilaresAsync(Guid ticketId, CancellationToken ct = default);
    Task RemoverSimilaridadeAsync(Guid similaridadeId, CancellationToken ct = default);

    // Parâmetros de classificação
    Task<List<ParametroClassificacao>> ListarParametrosAsync(string? tipo = null, CancellationToken ct = default);
    Task<ParametroClassificacao?> ObterParametroAsync(Guid id, CancellationToken ct = default);
    Task<ParametroClassificacao> CriarParametroAsync(ParametroClassificacao parametro, CancellationToken ct = default);
    Task<ParametroClassificacao?> AtualizarParametroAsync(Guid id, ParametroClassificacao dto, CancellationToken ct = default);
    Task RemoverParametroAsync(Guid id, CancellationToken ct = default);
}
