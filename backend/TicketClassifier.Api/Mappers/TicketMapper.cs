using TicketClassifier.Api.Dtos.Input;
using TicketClassifier.Api.Dtos.Output;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Mappers;

public static class TicketMapper
{
    public static Ticket ToEntity(TicketCsvInput input, Guid batchId, ClassificacaoResultado c, bool ok) => new()
    {
        BatchId = batchId,
        ExternalId = input.ExternalId,
        Assunto = input.Assunto,
        Descricao = input.Descricao,
        Categoria = c.Categoria,
        Prioridade = c.Prioridade,
        Departamento = c.Departamento,
        Resumo = c.Resumo,
        Confianca = c.Confianca,
        Justificativa = c.Justificativa,
        ProcessadoOk = ok,
    };

    /// <summary>Aplica um novo resultado a um ticket existente (reprocessamento).</summary>
    public static void AplicarResultado(Ticket t, ClassificacaoResultado c, bool ok)
    {
        t.Categoria = c.Categoria;
        t.Prioridade = c.Prioridade;
        t.Departamento = c.Departamento;
        t.Resumo = c.Resumo;
        t.Confianca = c.Confianca;
        t.Justificativa = c.Justificativa;
        t.ProcessadoOk = ok;
    }

    public static TicketDto ToDto(Ticket t) => new()
    {
        Id = t.Id,
        ExternalId = t.ExternalId,
        Assunto = t.Assunto,
        Descricao = t.Descricao,
        Categoria = t.Categoria,
        Prioridade = t.Prioridade,
        Departamento = t.Departamento,
        Resumo = t.Resumo,
        Confianca = t.Confianca,
        Justificativa = t.Justificativa,
        ProcessadoOk = t.ProcessadoOk,
    };

    public static EstatisticasDto ToEstatisticas(IReadOnlyCollection<Ticket> tickets) => new()
    {
        Total = tickets.Count,
        Falhas = tickets.Count(x => !x.ProcessadoOk),
        PorCategoria = Categorias.Lista.ToDictionary(c => c, c => tickets.Count(x => x.Categoria == c)),
        PorPrioridade = Categorias.Prioridades.ToDictionary(p => p, p => tickets.Count(x => x.Prioridade == p)),
        PorDepartamento = Categorias.Departamentos.ToDictionary(d => d, d => tickets.Count(x => x.Departamento == d)),
        ConfiancaMedia = tickets.Count > 0 ? Math.Round(tickets.Average(x => x.Confianca), 2) : 0,
    };

    public static BatchResumoDto ToResumo(TicketBatch b, IReadOnlyCollection<Ticket>? tickets = null) => new()
    {
        BatchId = b.Id,
        NomeArquivo = b.NomeArquivo,
        Total = b.Total,
        DataCriacao = b.DataCriacao,
        Estatisticas = tickets != null ? ToEstatisticas(tickets) : null,
    };

    public static BatchDetalheDto ToDetalhe(TicketBatch b, IReadOnlyCollection<Ticket> tickets) => new()
    {
        BatchId = b.Id,
        NomeArquivo = b.NomeArquivo,
        Total = b.Total,
        DataCriacao = b.DataCriacao,
        Estatisticas = ToEstatisticas(tickets),
        Tickets = tickets.Select(ToDto).ToList(),
    };
}
