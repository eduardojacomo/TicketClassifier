using Microsoft.EntityFrameworkCore;
using TicketClassifier.Api.Data;
using TicketClassifier.Api.Models;
using TicketClassifier.Api.Repositories.Interface;

namespace TicketClassifier.Api.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;

    public TicketRepository(AppDbContext db) => _db = db;

    public async Task<TicketBatch> AdicionarAsync(TicketBatch batch, CancellationToken ct = default)
    {
        _db.Batches.Add(batch);
        await _db.SaveChangesAsync(ct);
        return batch;
    }

    public async Task<TicketBatch?> ObterBatchAsync(Guid id, CancellationToken ct = default)
        => await _db.Batches.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<List<TicketBatch>> ListarBatchesAsync(CancellationToken ct = default)
        => await _db.Batches.OrderByDescending(b => b.DataCriacao).ToListAsync(ct);

    public async Task<List<Ticket>> ObterTicketsAsync(Guid batchId, CancellationToken ct = default)
        => await _db.Tickets.Where(t => t.BatchId == batchId).ToListAsync(ct);

    public async Task<List<Ticket>> ObterTicketsComFalhaAsync(Guid batchId, CancellationToken ct = default)
        => await _db.Tickets.Where(t => t.BatchId == batchId && !t.ProcessadoOk).ToListAsync(ct);

    public async Task AtualizarTicketsAsync(IEnumerable<Ticket> tickets, CancellationToken ct = default)
    {
        _db.Tickets.UpdateRange(tickets);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> BatchExisteAsync(Guid id, CancellationToken ct = default)
        => await _db.Batches.AnyAsync(b => b.Id == id, ct);
}
