using TicketClassifier.Api.Dtos.Output;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Mappers;
using TicketClassifier.Api.Models;
using TicketClassifier.Api.Prompts;
using TicketClassifier.Api.Repositories.Interface;
using TicketClassifier.Api.Services.Interface;

namespace TicketClassifier.Api.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _repo;
    private readonly CsvService _csv;
    private readonly IClassificationGatewayFactory _gatewayFactory;
    private readonly IProgressStore _progress;

    private readonly int _batchSize;
    private readonly int _parallelBatches;
    private readonly bool _singleBatch;

    public TicketService(
        ITicketRepository repo, CsvService csv,
        IClassificationGatewayFactory gatewayFactory, IProgressStore progress, IConfiguration cfg)
    {
        _repo = repo;
        _csv = csv;
        _gatewayFactory = gatewayFactory;
        _progress = progress;

        _batchSize       = cfg.GetValue<int?>("Processing:BatchSize") ?? 20;
        _parallelBatches = Math.Max(1, cfg.GetValue<int?>("Processing:ParallelBatches") ?? 3);
        _singleBatch     = cfg.GetValue<bool?>("Processing:SingleBatch") ?? false;
    }

    public async Task<BatchSummaryDto?> CheckDuplicateAsync(string fileName, CancellationToken ct = default)
    {
        var existing = await _repo.GetBatchByNameAsync(fileName, ct);
        if (existing is null) return null;
        return TicketMapper.ToSummary(existing);
    }

    public async Task<BatchSummaryDto> ProcessCsvAsync(Stream csv, string fileName, Guid jobId, bool overwrite = false, CancellationToken ct = default)
    {
        var inputs = _csv.Parse(csv);
        if (inputs.Count == 0)
            throw new InvalidOperationException("CSV is not in the expected format. Include columns as per the standard structure. e.g.: 'subject'/'description'.");

        if (overwrite)
        {
            var existing = await _repo.GetBatchByNameAsync(fileName, ct);
            if (existing is not null)
            {
                await _repo.RemoveSimilaritiesByBatchAsync(existing.Id, ct);
                await _repo.RemoveBatchAsync(existing.Id, ct);
            }
        }

        var size = (_singleBatch || _batchSize <= 0) ? inputs.Count : _batchSize;
        var totalBatches = (int)Math.Ceiling(inputs.Count / (double)size);
        _progress.Start(jobId, inputs.Count, totalBatches);

        var gateway = _gatewayFactory.Create();
        var batch = new TicketBatch { FileName = fileName, Total = inputs.Count };

        var results = await ClassifyAsync(
            gateway, inputs.Select(e => (e.Subject, e.Description)).ToList(), ct,
            onBatchCompleted: (proc, ok) => _progress.ReportBatch(jobId, proc, ok));

        batch.Tickets = inputs
            .Select((e, i) => TicketMapper.ToEntity(e, batch.Id, results[i].Result, results[i].Ok))
            .ToList();

        await _repo.AddAsync(batch, ct);
        await CalculateAndPersistSimilaritiesAsync(batch.Id, batch.Tickets, ct);

        _progress.Complete(jobId, batch.Id);
        return TicketMapper.ToSummary(batch, batch.Tickets);
    }

    public async Task<BatchDetailDto?> ReprocessAllAsync(Guid batchId, Guid jobId, CancellationToken ct = default)
    {
        var batch = await _repo.GetBatchAsync(batchId, ct);
        if (batch is null) return null;

        var tickets = await _repo.GetTicketsAsync(batchId, ct);
        if (tickets.Count == 0) return TicketMapper.ToDetail(batch, tickets);

        var size = (_singleBatch || _batchSize <= 0) ? tickets.Count : _batchSize;
        var totalBatches = (int)Math.Ceiling(tickets.Count / (double)size);
        _progress.Start(jobId, tickets.Count, totalBatches);

        var gateway = _gatewayFactory.Create();
        var results = await ClassifyAsync(
            gateway, tickets.Select(t => (t.Subject, t.Description)).ToList(), ct,
            onBatchCompleted: (proc, ok) => _progress.ReportBatch(jobId, proc, ok));

        for (var i = 0; i < tickets.Count; i++)
        {
            TicketMapper.ApplyResult(tickets[i], results[i].Result, results[i].Ok);
            tickets[i].RecordModified = false;
            tickets[i].ModifiedDate = null;
        }

        await _repo.UpdateTicketsAsync(tickets, ct);
        await _repo.RemoveSimilaritiesByBatchAsync(batchId, ct);
        await CalculateAndPersistSimilaritiesAsync(batchId, tickets, ct);
        _progress.Complete(jobId, batchId);

        return await GetAsync(batchId, ct);
    }

    public async Task<BatchDetailDto?> ReprocessFailuresAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _repo.GetBatchAsync(batchId, ct);
        if (batch is null) return null;

        var failedTickets = await _repo.GetFailedTicketsAsync(batchId, ct);
        if (failedTickets.Count > 0)
        {
            var gateway = _gatewayFactory.Create();
            var results = await ClassifyAsync(
                gateway, failedTickets.Select(t => (t.Subject, t.Description)).ToList(), ct);

            for (var i = 0; i < failedTickets.Count; i++)
                TicketMapper.ApplyResult(failedTickets[i], results[i].Result, results[i].Ok);

            await _repo.UpdateTicketsAsync(failedTickets, ct);

            var allTickets = await _repo.GetTicketsAsync(batchId, ct);
            await _repo.RemoveSimilaritiesByBatchAsync(batchId, ct);
            await CalculateAndPersistSimilaritiesAsync(batchId, allTickets, ct);
        }

        return await GetAsync(batchId, ct);
    }

    // ── Persisted similarities ────────────────────────────────────────

    private async Task CalculateAndPersistSimilaritiesAsync(Guid batchId, IReadOnlyList<Ticket> tickets, CancellationToken ct)
    {
        var parsed = tickets
            .Select(t => (ticket: t, tags: ParseTags(t.Tags)))
            .ToList();

        var similarities = new List<TicketSimilarity>();

        for (var i = 0; i < parsed.Count; i++)
        {
            for (var j = i + 1; j < parsed.Count; j++)
            {
                var (a, tagsA) = parsed[i];
                var (b, tagsB) = parsed[j];

                if (a.Category != b.Category || a.Department != b.Department)
                    continue;

                var shared = tagsA.Intersect(tagsB).ToArray();
                if (shared.Length == 0)
                    continue;

                var tagsJson = System.Text.Json.JsonSerializer.Serialize(shared);
                similarities.Add(new TicketSimilarity
                {
                    SourceTicketId = a.Id,
                    RelatedTicketId = b.Id,
                    SharedTags = tagsJson,
                });
            }
        }

        if (similarities.Count > 0)
            await _repo.AddSimilaritiesAsync(similarities, ct);
    }

    public async Task<List<TicketDto>> GetSimilarAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _repo.GetTicketAsync(ticketId, ct);
        if (ticket is null) return new();

        var similarities = await _repo.GetSimilaritiesAsync(ticketId, ct);
        var relatedIds = similarities
            .Select(s => s.SourceTicketId == ticketId ? s.RelatedTicketId : s.SourceTicketId)
            .Distinct()
            .ToList();

        if (relatedIds.Count == 0) return new();

        var batchTickets = await _repo.GetTicketsAsync(ticket.BatchId, ct);
        return batchTickets
            .Where(t => relatedIds.Contains(t.Id))
            .Select(t => TicketMapper.ToDto(t))
            .ToList();
    }

    public async Task RemoveSimilarityAsync(Guid similarityId, CancellationToken ct = default)
        => await _repo.RemoveSimilarityAsync(similarityId, ct);

    // ── Batch classification ───────────────────────────────────────────

    private async Task<ClassificationPromptBuilder> CreatePromptBuilderAsync(CancellationToken ct)
    {
        var parameters = await _repo.ListParametersAsync(null, ct);
        return new ClassificationPromptBuilder(parameters);
    }

    private async Task<IReadOnlyList<(ClassificationResult Result, bool Ok)>> ClassifyAsync(
        IClassificationGateway gateway,
        IReadOnlyList<(string? Subject, string Description)> inputs,
        CancellationToken ct,
        Action<int, int>? onBatchCompleted = null)
    {
        var n = inputs.Count;
        var size = (_singleBatch || _batchSize <= 0) ? n : _batchSize;

        var items = inputs
            .Select((e, i) => new TicketToClassify(i, e.Subject, e.Description))
            .ToList();

        var batches = items
            .GroupBy(x => x.Index / size)
            .Select(g => g.ToList())
            .ToList();

        var output = new (ClassificationResult, bool)[n];
        var throttler = new SemaphoreSlim(_parallelBatches);
        var promptBuilder = await CreatePromptBuilderAsync(ct);

        await Task.WhenAll(batches.Select(async batch =>
        {
            await throttler.WaitAsync(ct);
            try
            {
                var batchNumber = batches.IndexOf(batch) + 1;
                var results = await gateway.ClassifyBatchAsync(batch, promptBuilder, ct, batchNumber, batches.Count, n);
                var okInBatch = 0;
                for (var k = 0; k < batch.Count; k++)
                {
                    var idx = batch[k].Index;
                    var r = k < results.Count ? results[k] : Categories.Fallback;
                    var ok = !Categories.IsFallback(r);
                    if (ok) okInBatch++;
                    output[idx] = (r, ok);
                }
                onBatchCompleted?.Invoke(batch.Count, okInBatch);
            }
            finally { throttler.Release(); }
        }));

        return output;
    }

    public async Task<List<BatchSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var batches = await _repo.ListBatchesAsync(ct);
        return batches.Select(b => TicketMapper.ToSummary(b)).ToList();
    }

    public async Task<BatchDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var batch = await _repo.GetBatchAsync(id, ct);
        if (batch is null) return null;
        var tickets = await _repo.GetTicketsAsync(id, ct);

        var similarityCount = new Dictionary<Guid, int>();
        foreach (var t in tickets)
            similarityCount[t.Id] = await _repo.CountSimilarAsync(t.Id, ct);

        return TicketMapper.ToDetail(batch, tickets, similarityCount);
    }

    public async Task<byte[]?> ExportAsync(Guid id, string[]? columns = null, CancellationToken ct = default)
    {
        if (!await _repo.BatchExistsAsync(id, ct)) return null;
        var tickets = await _repo.GetTicketsAsync(id, ct);
        return _csv.Export(tickets, columns);
    }

    public async Task<TicketDto?> UpdateTicketAsync(Guid ticketId, TicketEditDto dto, CancellationToken ct = default)
    {
        var ticket = await _repo.GetTicketAsync(ticketId, ct);
        if (ticket is null) return null;

        if (dto.Category != null) ticket.Category = Categories.ValidCategory(dto.Category);
        if (dto.Priority != null) ticket.Priority = Categories.ValidPriority(dto.Priority);
        if (dto.Department != null) ticket.Department = Categories.ValidDepartment(dto.Department);
        if (dto.Sentiment != null) ticket.Sentiment = Categories.ValidSentiment(dto.Sentiment);
        if (dto.Tags != null) ticket.Tags = System.Text.Json.JsonSerializer.Serialize(dto.Tags);

        ticket.RecordModified = true;
        ticket.ModifiedDate = DateTime.UtcNow;

        await _repo.UpdateTicketAsync(ticket, ct);
        return TicketMapper.ToDto(ticket);
    }

    // ── Classification parameters (CRUD) ───────────────────────────────

    public async Task<List<ClassificationParameter>> ListParametersAsync(string? type = null, CancellationToken ct = default)
        => await _repo.ListParametersAsync(type, ct);

    public async Task<ClassificationParameter?> GetParameterAsync(Guid id, CancellationToken ct = default)
        => await _repo.GetParameterAsync(id, ct);

    public async Task<ClassificationParameter> CreateParameterAsync(ClassificationParameter parameter, CancellationToken ct = default)
    {
        await _repo.AddParameterAsync(parameter, ct);
        return parameter;
    }

    public async Task<ClassificationParameter?> UpdateParameterAsync(Guid id, ClassificationParameter dto, CancellationToken ct = default)
    {
        var existing = await _repo.GetParameterAsync(id, ct);
        if (existing is null) return null;

        existing.Type = dto.Type;
        existing.Term = dto.Term;
        existing.Target = dto.Target;
        existing.Active = dto.Active;

        await _repo.UpdateParameterAsync(existing, ct);
        return existing;
    }

    public async Task RemoveParameterAsync(Guid id, CancellationToken ct = default)
        => await _repo.RemoveParameterAsync(id, ct);

    private static HashSet<string> ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return new(System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>()); }
        catch { return new(); }
    }
}
