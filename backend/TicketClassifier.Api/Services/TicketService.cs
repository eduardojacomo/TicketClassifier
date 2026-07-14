using TicketClassifier.Api.Dtos.Output;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Mappers;
using TicketClassifier.Api.Models;
using TicketClassifier.Api.Repositories.Interface;
using TicketClassifier.Api.Services.Interface;

namespace TicketClassifier.Api.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _repo;
    private readonly CsvService _csv;
    private readonly IClassificacaoGatewayFactory _gatewayFactory;
    private readonly IProgressoStore _progresso;

    private readonly int _tamanhoLote;
    private readonly int _lotesParalelos;
    private readonly bool _loteUnico;

    public TicketService(
        ITicketRepository repo, CsvService csv,
        IClassificacaoGatewayFactory gatewayFactory, IProgressoStore progresso, IConfiguration cfg)
    {
        _repo = repo;
        _csv = csv;
        _gatewayFactory = gatewayFactory;
        _progresso = progresso;

        _tamanhoLote    = cfg.GetValue<int?>("Processing:TamanhoLote") ?? 20;
        _lotesParalelos = Math.Max(1, cfg.GetValue<int?>("Processing:LotesParalelos") ?? 3);
        _loteUnico      = cfg.GetValue<bool?>("Processing:LoteUnico") ?? false;
    }

    public async Task<BatchResumoDto> ProcessarCsvAsync(Stream csv, string nomeArquivo, Guid jobId, CancellationToken ct = default)
    {
        var entradas = _csv.Parse(csv);
        if (entradas.Count == 0)
            throw new InvalidOperationException("CSV fora da estrutura padrão. Inclua colunas de acordo com o padrão estabelecido. ex: 'subject'/'description'.");

        var tamanho = (_loteUnico || _tamanhoLote <= 0) ? entradas.Count : _tamanhoLote;
        var totalLotes = (int)Math.Ceiling(entradas.Count / (double)tamanho);
        _progresso.Iniciar(jobId, entradas.Count, totalLotes);

        var gateway = _gatewayFactory.Criar();
        var batch = new TicketBatch { NomeArquivo = nomeArquivo, Total = entradas.Count };

        var resultados = await ClassificarAsync(
            gateway, entradas.Select(e => (e.Assunto, e.Descricao)).ToList(), ct,
            onLoteConcluido: (proc, ok) => _progresso.ReportarLote(jobId, proc, ok));

        batch.Tickets = entradas
            .Select((e, i) => TicketMapper.ToEntity(e, batch.Id, resultados[i].Resultado, resultados[i].Ok))
            .ToList();

        await _repo.AdicionarAsync(batch, ct);
        _progresso.Concluir(jobId, batch.Id);
        return TicketMapper.ToResumo(batch, batch.Tickets);
    }

    /// <summary>Reclassifica apenas os tickets que falharam (fallback) no lote.</summary>
    public async Task<BatchDetalheDto?> ReprocessarFalhasAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await _repo.ObterBatchAsync(batchId, ct);
        if (batch is null) return null;

        var falhas = await _repo.ObterTicketsComFalhaAsync(batchId, ct);
        if (falhas.Count > 0)
        {
            var gateway = _gatewayFactory.Criar();
            var resultados = await ClassificarAsync(
                gateway, falhas.Select(t => (t.Assunto, t.Descricao)).ToList(), ct);

            for (var i = 0; i < falhas.Count; i++)
                TicketMapper.AplicarResultado(falhas[i], resultados[i].Resultado, resultados[i].Ok);

            await _repo.AtualizarTicketsAsync(falhas, ct);
        }

        var tickets = await _repo.ObterTicketsAsync(batchId, ct);
        return TicketMapper.ToDetalhe(batch, tickets);
    }

    // ── Classificação em lotes (config: tamanho, paralelismo, lote único) ────────
    private async Task<IReadOnlyList<(ClassificacaoResultado Resultado, bool Ok)>> ClassificarAsync(
        IClassificacaoGateway gateway,
        IReadOnlyList<(string? Assunto, string Descricao)> entradas,
        CancellationToken ct,
        Action<int, int>? onLoteConcluido = null)
    {
        var n = entradas.Count;
        var tamanho = (_loteUnico || _tamanhoLote <= 0) ? n : _tamanhoLote;

        var itens = entradas
            .Select((e, i) => new TicketParaClassificar(i, e.Assunto, e.Descricao))
            .ToList();

        var lotes = itens
            .GroupBy(x => x.Indice / tamanho)
            .Select(g => g.ToList())
            .ToList();

        var saida = new (ClassificacaoResultado, bool)[n];
        var throttler = new SemaphoreSlim(_lotesParalelos);

        await Task.WhenAll(lotes.Select(async lote =>
        {
            await throttler.WaitAsync(ct);
            try
            {
                var resultados = await gateway.ClassificarLoteAsync(lote, ct);
                var okNoLote = 0;
                for (var k = 0; k < lote.Count; k++)
                {
                    var idx = lote[k].Indice;
                    var r = k < resultados.Count ? resultados[k] : Categorias.Fallback;
                    var ok = !ReferenceEquals(r, Categorias.Fallback);
                    if (ok) okNoLote++;
                    saida[idx] = (r, ok);
                }
                onLoteConcluido?.Invoke(lote.Count, okNoLote);
            }
            finally { throttler.Release(); }
        }));

        return saida;
    }

    public async Task<List<BatchResumoDto>> ListarAsync(CancellationToken ct = default)
    {
        var batches = await _repo.ListarBatchesAsync(ct);
        return batches.Select(b => TicketMapper.ToResumo(b)).ToList();
    }

    public async Task<BatchDetalheDto?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var batch = await _repo.ObterBatchAsync(id, ct);
        if (batch is null) return null;
        var tickets = await _repo.ObterTicketsAsync(id, ct);
        return TicketMapper.ToDetalhe(batch, tickets);
    }

    public async Task<byte[]?> ExportarAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _repo.BatchExisteAsync(id, ct)) return null;
        var tickets = await _repo.ObterTicketsAsync(id, ct);
        return _csv.Export(tickets);
    }
}
