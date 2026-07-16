using TicketClassifier.Api.Dtos.Output;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Services.Interface;

public interface ITicketService
{
    Task<BatchSummaryDto?> CheckDuplicateAsync(string fileName, CancellationToken ct = default);
    Task<BatchSummaryDto> ProcessCsvAsync(Stream csv, string fileName, Guid jobId, bool overwrite = false, CancellationToken ct = default);
    Task<BatchDetailDto?> ReprocessFailuresAsync(Guid batchId, CancellationToken ct = default);
    Task<BatchDetailDto?> ReprocessAllAsync(Guid batchId, Guid jobId, CancellationToken ct = default);
    Task<List<BatchSummaryDto>> ListAsync(CancellationToken ct = default);
    Task<BatchDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<byte[]?> ExportAsync(Guid id, string[]? columns = null, CancellationToken ct = default);
    Task<TicketDto?> UpdateTicketAsync(Guid ticketId, TicketEditDto dto, CancellationToken ct = default);
    Task<List<TicketDto>> GetSimilarAsync(Guid ticketId, CancellationToken ct = default);
    Task RemoveSimilarityAsync(Guid similarityId, CancellationToken ct = default);

    // Classification parameters
    Task<List<ClassificationParameter>> ListParametersAsync(string? type = null, CancellationToken ct = default);
    Task<ClassificationParameter?> GetParameterAsync(Guid id, CancellationToken ct = default);
    Task<ClassificationParameter> CreateParameterAsync(ClassificationParameter parameter, CancellationToken ct = default);
    Task<ClassificationParameter?> UpdateParameterAsync(Guid id, ClassificationParameter dto, CancellationToken ct = default);
    Task RemoveParameterAsync(Guid id, CancellationToken ct = default);
}
