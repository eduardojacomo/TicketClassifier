using Microsoft.EntityFrameworkCore;
using TicketClassifier.Api.Data;
using TicketClassifier.Api.Models;
using TicketClassifier.Api.Repositories.Interface;

namespace TicketClassifier.Api.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketRepository(AppDbContext db) => _db = db;

    public async Task<TicketBatch> AddAsync(TicketBatch batch, CancellationToken ct = default)
    {
        _db.Batches.Add(batch);
        await _db.SaveChangesAsync(ct);
        return batch;
    }

    public async Task<TicketBatch?> GetBatchAsync(Guid id, CancellationToken ct = default)
        => await _db.Batches.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<List<TicketBatch>> ListBatchesAsync(CancellationToken ct = default)
        => await _db.Batches.OrderByDescending(b => b.CreatedDate).ToListAsync(ct);

    public async Task<List<Ticket>> GetTicketsAsync(Guid batchId, CancellationToken ct = default)
        => await _db.Tickets.Where(t => t.BatchId == batchId).ToListAsync(ct);

    public async Task<List<Ticket>> GetFailedTicketsAsync(Guid batchId, CancellationToken ct = default)
        => await _db.Tickets.Where(t => t.BatchId == batchId && !t.ProcessedOk).ToListAsync(ct);

    public async Task UpdateTicketsAsync(IEnumerable<Ticket> tickets, CancellationToken ct = default)
    {
        _db.Tickets.UpdateRange(tickets);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> BatchExistsAsync(Guid id, CancellationToken ct = default)
        => await _db.Batches.AnyAsync(b => b.Id == id, ct);

    public async Task<TicketBatch?> GetBatchByNameAsync(string fileName, CancellationToken ct = default)
        => await _db.Batches
            .Where(b => b.FileName == fileName)
            .OrderByDescending(b => b.CreatedDate)
            .FirstOrDefaultAsync(ct);

    public async Task RemoveBatchAsync(Guid id, CancellationToken ct = default)
    {
        var batch = await _db.Batches.FindAsync(new object[] { id }, ct);
        if (batch is not null)
        {
            _db.Batches.Remove(batch);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<Ticket?> GetTicketAsync(Guid ticketId, CancellationToken ct = default)
        => await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);

    public async Task UpdateTicketAsync(Ticket ticket, CancellationToken ct = default)
    {
        _db.Tickets.Update(ticket);
        await _db.SaveChangesAsync(ct);
    }

    // ── Similarities ────────────────────────────────────────────────────

    public async Task AddSimilaritiesAsync(IEnumerable<TicketSimilarity> similarities, CancellationToken ct = default)
    {
        _db.Similarities.AddRange(similarities);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveSimilaritiesByBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        var ticketIds = await _db.Tickets
            .Where(t => t.BatchId == batchId)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var similarities = await _db.Similarities
            .Where(s => ticketIds.Contains(s.SourceTicketId))
            .ToListAsync(ct);

        if (similarities.Count > 0)
        {
            _db.Similarities.RemoveRange(similarities);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<TicketSimilarity>> GetSimilaritiesAsync(Guid ticketId, CancellationToken ct = default)
        => await _db.Similarities
            .Where(s => s.SourceTicketId == ticketId || s.RelatedTicketId == ticketId)
            .ToListAsync(ct);

    public async Task RemoveSimilarityAsync(Guid id, CancellationToken ct = default)
    {
        var sim = await _db.Similarities.FindAsync(new object[] { id }, ct);
        if (sim is not null)
        {
            _db.Similarities.Remove(sim);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CountSimilarAsync(Guid ticketId, CancellationToken ct = default)
        => await _db.Similarities
            .CountAsync(s => s.SourceTicketId == ticketId || s.RelatedTicketId == ticketId, ct);

    // ── Classification parameters ──────────────────────────────────────

    public async Task<List<ClassificationParameter>> ListParametersAsync(string? type = null, CancellationToken ct = default)
    {
        var query = _db.ClassificationParameters.AsQueryable();
        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.Type == type);
        return await query.OrderBy(p => p.Type).ThenBy(p => p.Term).ToListAsync(ct);
    }

    public async Task<ClassificationParameter?> GetParameterAsync(Guid id, CancellationToken ct = default)
        => await _db.ClassificationParameters.FindAsync(new object[] { id }, ct);

    public async Task AddParameterAsync(ClassificationParameter parameter, CancellationToken ct = default)
    {
        _db.ClassificationParameters.Add(parameter);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateParameterAsync(ClassificationParameter parameter, CancellationToken ct = default)
    {
        _db.ClassificationParameters.Update(parameter);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveParameterAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.ClassificationParameters.FindAsync(new object[] { id }, ct);
        if (p is not null)
        {
            _db.ClassificationParameters.Remove(p);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CountParametersAsync(CancellationToken ct = default)
        => await _db.ClassificationParameters.CountAsync(ct);

    public async Task AddParametersAsync(IEnumerable<ClassificationParameter> parameters, CancellationToken ct = default)
    {
        _db.ClassificationParameters.AddRange(parameters);
        await _db.SaveChangesAsync(ct);
    }
}
