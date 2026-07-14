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
        Sentimento = c.Sentimento,
        Tags = System.Text.Json.JsonSerializer.Serialize(c.Tags),
        ProcessadoOk = ok,
    };

    public static void AplicarResultado(Ticket t, ClassificacaoResultado c, bool ok)
    {
        t.Categoria = c.Categoria;
        t.Prioridade = c.Prioridade;
        t.Departamento = c.Departamento;
        t.Resumo = c.Resumo;
        t.Confianca = c.Confianca;
        t.Justificativa = c.Justificativa;
        t.Sentimento = c.Sentimento;
        t.Tags = System.Text.Json.JsonSerializer.Serialize(c.Tags);
        t.ProcessadoOk = ok;
    }

    public static TicketDto ToDto(Ticket t, int similares = 0) => new()
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
        Sentimento = t.Sentimento,
        Tags = ParseTags(t.Tags),
        ProcessadoOk = t.ProcessadoOk,
        RegistroModificado = t.RegistroModificado,
        DataModificacao = t.DataModificacao,
        Similares = similares,
    };

    private static string[] ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try { return System.Text.Json.JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>(); }
        catch { return Array.Empty<string>(); }
    }

    public static EstatisticasDto ToEstatisticas(IReadOnlyCollection<Ticket> tickets) => new()
    {
        Total = tickets.Count,
        Falhas = tickets.Count(x => !x.ProcessadoOk),
        PorCategoria = Categorias.Lista.ToDictionary(c => c, c => tickets.Count(x => x.Categoria == c)),
        PorPrioridade = Categorias.Prioridades.ToDictionary(p => p, p => tickets.Count(x => x.Prioridade == p)),
        PorDepartamento = Categorias.Departamentos.ToDictionary(d => d, d => tickets.Count(x => x.Departamento == d)),
        PorSentimento = Categorias.Sentimentos.ToDictionary(s => s, s => tickets.Count(x => x.Sentimento == s)),
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

    public static BatchDetalheDto ToDetalhe(TicketBatch b, IReadOnlyCollection<Ticket> tickets, Dictionary<Guid, int>? similaridadeCount = null)
    {
        return new()
        {
            BatchId = b.Id,
            NomeArquivo = b.NomeArquivo,
            Total = b.Total,
            DataCriacao = b.DataCriacao,
            Estatisticas = ToEstatisticas(tickets),
            Tickets = tickets.Select(t => ToDto(t, similaridadeCount?.GetValueOrDefault(t.Id) ?? 0)).ToList(),
        };
    }
}
