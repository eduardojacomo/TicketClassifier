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

    public async Task<TicketBatch?> ObterBatchPorNomeAsync(string nomeArquivo, CancellationToken ct = default)
        => await _db.Batches
            .Where(b => b.NomeArquivo == nomeArquivo)
            .OrderByDescending(b => b.DataCriacao)
            .FirstOrDefaultAsync(ct);

    public async Task RemoverBatchAsync(Guid id, CancellationToken ct = default)
    {
        var batch = await _db.Batches.FindAsync(new object[] { id }, ct);
        if (batch is not null)
        {
            _db.Batches.Remove(batch);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<Ticket?> ObterTicketAsync(Guid ticketId, CancellationToken ct = default)
        => await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);

    public async Task AtualizarTicketAsync(Ticket ticket, CancellationToken ct = default)
    {
        _db.Tickets.Update(ticket);
        await _db.SaveChangesAsync(ct);
    }

    // ── Similaridades ────────────────────────────────────────────────────

    public async Task AdicionarSimilaridadesAsync(IEnumerable<TicketSimilaridade> similaridades, CancellationToken ct = default)
    {
        _db.Similaridades.AddRange(similaridades);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoverSimilaridadesPorBatchAsync(Guid batchId, CancellationToken ct = default)
    {
        var ticketIds = await _db.Tickets
            .Where(t => t.BatchId == batchId)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var similaridades = await _db.Similaridades
            .Where(s => ticketIds.Contains(s.TicketOrigemId))
            .ToListAsync(ct);

        if (similaridades.Count > 0)
        {
            _db.Similaridades.RemoveRange(similaridades);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<List<TicketSimilaridade>> ObterSimilaridadesAsync(Guid ticketId, CancellationToken ct = default)
        => await _db.Similaridades
            .Where(s => s.TicketOrigemId == ticketId || s.TicketRelacionadoId == ticketId)
            .ToListAsync(ct);

    public async Task RemoverSimilaridadeAsync(Guid id, CancellationToken ct = default)
    {
        var sim = await _db.Similaridades.FindAsync(new object[] { id }, ct);
        if (sim is not null)
        {
            _db.Similaridades.Remove(sim);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> ContarSimilaresAsync(Guid ticketId, CancellationToken ct = default)
        => await _db.Similaridades
            .CountAsync(s => s.TicketOrigemId == ticketId || s.TicketRelacionadoId == ticketId, ct);

    // ── Parâmetros de classificação ──────────────────────────────────────

    public async Task<List<ParametroClassificacao>> ListarParametrosAsync(string? tipo = null, CancellationToken ct = default)
    {
        var q = _db.ParametrosClassificacao.AsQueryable();
        if (!string.IsNullOrEmpty(tipo))
            q = q.Where(p => p.Tipo == tipo);
        return await q.OrderBy(p => p.Tipo).ThenBy(p => p.Termo).ToListAsync(ct);
    }

    public async Task<ParametroClassificacao?> ObterParametroAsync(Guid id, CancellationToken ct = default)
        => await _db.ParametrosClassificacao.FindAsync(new object[] { id }, ct);

    public async Task AdicionarParametroAsync(ParametroClassificacao parametro, CancellationToken ct = default)
    {
        _db.ParametrosClassificacao.Add(parametro);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AtualizarParametroAsync(ParametroClassificacao parametro, CancellationToken ct = default)
    {
        _db.ParametrosClassificacao.Update(parametro);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoverParametroAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.ParametrosClassificacao.FindAsync(new object[] { id }, ct);
        if (p is not null)
        {
            _db.ParametrosClassificacao.Remove(p);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> ContarParametrosAsync(CancellationToken ct = default)
        => await _db.ParametrosClassificacao.CountAsync(ct);

    public async Task AdicionarParametrosAsync(IEnumerable<ParametroClassificacao> parametros, CancellationToken ct = default)
    {
        _db.ParametrosClassificacao.AddRange(parametros);
        await _db.SaveChangesAsync(ct);
    }
}
