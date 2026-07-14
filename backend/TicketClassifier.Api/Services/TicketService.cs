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

    public async Task<BatchResumoDto?> VerificarDuplicataAsync(string nomeArquivo, CancellationToken ct = default)
    {
        var existente = await _repo.ObterBatchPorNomeAsync(nomeArquivo, ct);
        if (existente is null) return null;
        return TicketMapper.ToResumo(existente);
    }

    public async Task<BatchResumoDto> ProcessarCsvAsync(Stream csv, string nomeArquivo, Guid jobId, bool sobrescrever = false, CancellationToken ct = default)
    {
        var entradas = _csv.Parse(csv);
        if (entradas.Count == 0)
            throw new InvalidOperationException("CSV fora da estrutura padrão. Inclua colunas de acordo com o padrão estabelecido. ex: 'subject'/'description'.");

        if (sobrescrever)
        {
            var existente = await _repo.ObterBatchPorNomeAsync(nomeArquivo, ct);
            if (existente is not null)
            {
                await _repo.RemoverSimilaridadesPorBatchAsync(existente.Id, ct);
                await _repo.RemoverBatchAsync(existente.Id, ct);
            }
        }

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
        await CalcularEPersistirSimilaridadesAsync(batch.Id, batch.Tickets, ct);

        _progresso.Concluir(jobId, batch.Id);
        return TicketMapper.ToResumo(batch, batch.Tickets);
    }

    public async Task<BatchDetalheDto?> ReprocessarTudoAsync(Guid batchId, Guid jobId, CancellationToken ct = default)
    {
        var batch = await _repo.ObterBatchAsync(batchId, ct);
        if (batch is null) return null;

        var tickets = await _repo.ObterTicketsAsync(batchId, ct);
        if (tickets.Count == 0) return TicketMapper.ToDetalhe(batch, tickets);

        var tamanho = (_loteUnico || _tamanhoLote <= 0) ? tickets.Count : _tamanhoLote;
        var totalLotes = (int)Math.Ceiling(tickets.Count / (double)tamanho);
        _progresso.Iniciar(jobId, tickets.Count, totalLotes);

        var gateway = _gatewayFactory.Criar();
        var resultados = await ClassificarAsync(
            gateway, tickets.Select(t => (t.Assunto, t.Descricao)).ToList(), ct,
            onLoteConcluido: (proc, ok) => _progresso.ReportarLote(jobId, proc, ok));

        for (var i = 0; i < tickets.Count; i++)
        {
            TicketMapper.AplicarResultado(tickets[i], resultados[i].Resultado, resultados[i].Ok);
            tickets[i].RegistroModificado = false;
            tickets[i].DataModificacao = null;
        }

        await _repo.AtualizarTicketsAsync(tickets, ct);
        await _repo.RemoverSimilaridadesPorBatchAsync(batchId, ct);
        await CalcularEPersistirSimilaridadesAsync(batchId, tickets, ct);
        _progresso.Concluir(jobId, batchId);

        return await ObterAsync(batchId, ct);
    }

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

            var todosTickets = await _repo.ObterTicketsAsync(batchId, ct);
            await _repo.RemoverSimilaridadesPorBatchAsync(batchId, ct);
            await CalcularEPersistirSimilaridadesAsync(batchId, todosTickets, ct);
        }

        return await ObterAsync(batchId, ct);
    }

    // ── Similaridades persistidas ────────────────────────────────────────

    private async Task CalcularEPersistirSimilaridadesAsync(Guid batchId, IReadOnlyList<Ticket> tickets, CancellationToken ct)
    {
        var parsed = tickets
            .Select(t => (ticket: t, tags: ParseTags(t.Tags)))
            .ToList();

        var similaridades = new List<TicketSimilaridade>();

        for (var i = 0; i < parsed.Count; i++)
        {
            for (var j = i + 1; j < parsed.Count; j++)
            {
                var (a, tagsA) = parsed[i];
                var (b, tagsB) = parsed[j];

                if (a.Categoria != b.Categoria || a.Departamento != b.Departamento)
                    continue;

                var compartilhadas = tagsA.Intersect(tagsB).ToArray();
                if (compartilhadas.Length == 0)
                    continue;

                var tagsJson = System.Text.Json.JsonSerializer.Serialize(compartilhadas);
                similaridades.Add(new TicketSimilaridade
                {
                    TicketOrigemId = a.Id,
                    TicketRelacionadoId = b.Id,
                    TagsCompartilhadas = tagsJson,
                });
            }
        }

        if (similaridades.Count > 0)
            await _repo.AdicionarSimilaridadesAsync(similaridades, ct);
    }

    public async Task<List<TicketDto>> ObterSimilaresAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _repo.ObterTicketAsync(ticketId, ct);
        if (ticket is null) return new();

        var similaridades = await _repo.ObterSimilaridadesAsync(ticketId, ct);
        var idsRelacionados = similaridades
            .Select(s => s.TicketOrigemId == ticketId ? s.TicketRelacionadoId : s.TicketOrigemId)
            .Distinct()
            .ToList();

        if (idsRelacionados.Count == 0) return new();

        var batchTickets = await _repo.ObterTicketsAsync(ticket.BatchId, ct);
        return batchTickets
            .Where(t => idsRelacionados.Contains(t.Id))
            .Select(t => TicketMapper.ToDto(t))
            .ToList();
    }

    public async Task RemoverSimilaridadeAsync(Guid similaridadeId, CancellationToken ct = default)
        => await _repo.RemoverSimilaridadeAsync(similaridadeId, ct);

    // ── Classificação em lotes ───────────────────────────────────────────

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
                var loteNum = lotes.IndexOf(lote) + 1;
                var resultados = await gateway.ClassificarLoteAsync(lote, ct, loteNum, lotes.Count, n);
                var okNoLote = 0;
                for (var k = 0; k < lote.Count; k++)
                {
                    var idx = lote[k].Indice;
                    var r = k < resultados.Count ? resultados[k] : Categorias.Fallback;
                    var ok = !Categorias.EhFallback(r);
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

        var similaridadeCount = new Dictionary<Guid, int>();
        foreach (var t in tickets)
            similaridadeCount[t.Id] = await _repo.ContarSimilaresAsync(t.Id, ct);

        return TicketMapper.ToDetalhe(batch, tickets, similaridadeCount);
    }

    public async Task<byte[]?> ExportarAsync(Guid id, string[]? colunas = null, CancellationToken ct = default)
    {
        if (!await _repo.BatchExisteAsync(id, ct)) return null;
        var tickets = await _repo.ObterTicketsAsync(id, ct);
        return _csv.Export(tickets, colunas);
    }

    public async Task<TicketDto?> AtualizarTicketAsync(Guid ticketId, TicketEditDto dto, CancellationToken ct = default)
    {
        var ticket = await _repo.ObterTicketAsync(ticketId, ct);
        if (ticket is null) return null;

        if (dto.Categoria != null) ticket.Categoria = Categorias.CategoriaValida(dto.Categoria);
        if (dto.Prioridade != null) ticket.Prioridade = Categorias.PrioridadeValida(dto.Prioridade);
        if (dto.Departamento != null) ticket.Departamento = Categorias.DepartamentoValido(dto.Departamento);
        if (dto.Sentimento != null) ticket.Sentimento = Categorias.SentimentoValido(dto.Sentimento);
        if (dto.Tags != null) ticket.Tags = System.Text.Json.JsonSerializer.Serialize(dto.Tags);

        ticket.RegistroModificado = true;
        ticket.DataModificacao = DateTime.UtcNow;

        await _repo.AtualizarTicketAsync(ticket, ct);
        return TicketMapper.ToDto(ticket);
    }

    // ── Parâmetros de classificação (CRUD) ───────────────────────────────

    public async Task<List<ParametroClassificacao>> ListarParametrosAsync(string? tipo = null, CancellationToken ct = default)
        => await _repo.ListarParametrosAsync(tipo, ct);

    public async Task<ParametroClassificacao?> ObterParametroAsync(Guid id, CancellationToken ct = default)
        => await _repo.ObterParametroAsync(id, ct);

    public async Task<ParametroClassificacao> CriarParametroAsync(ParametroClassificacao parametro, CancellationToken ct = default)
    {
        await _repo.AdicionarParametroAsync(parametro, ct);
        return parametro;
    }

    public async Task<ParametroClassificacao?> AtualizarParametroAsync(Guid id, ParametroClassificacao dto, CancellationToken ct = default)
    {
        var existente = await _repo.ObterParametroAsync(id, ct);
        if (existente is null) return null;

        existente.Tipo = dto.Tipo;
        existente.Termo = dto.Termo;
        existente.Alvo = dto.Alvo;
        existente.Ativo = dto.Ativo;

        await _repo.AtualizarParametroAsync(existente, ct);
        return existente;
    }

    public async Task RemoverParametroAsync(Guid id, CancellationToken ct = default)
        => await _repo.RemoverParametroAsync(id, ct);

    private static HashSet<string> ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try { return new(System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>()); }
        catch { return new(); }
    }
}
