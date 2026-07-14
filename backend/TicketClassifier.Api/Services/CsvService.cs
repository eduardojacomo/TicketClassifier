using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TicketClassifier.Api.Dtos.Input;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Services;

/// <summary>Leitura e geração de CSV (parse flexível + export enriquecido).</summary>
public class CsvService
{
    public static readonly (string Key, string Label)[] ColunasDisponiveis =
    {
        ("id",              "ID"),
        ("assunto",         "Assunto"),
        ("descricao",       "Descrição"),
        ("categoria",       "Categoria"),
        ("prioridade",      "Prioridade"),
        ("departamento",    "Departamento"),
        ("sentimento",      "Sentimento"),
        ("tags",            "Tags"),
        ("resumo",          "Resumo"),
        ("confianca",       "Confiança"),
        ("justificativa",   "Justificativa"),
        ("modificado",      "Modificado"),
        ("dataModificacao", "Data Modificação"),
    };

    private static readonly HashSet<string> TodasAsChaves =
        new(ColunasDisponiveis.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);

    public List<TicketCsvInput> Parse(Stream csv)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            DetectDelimiter = true,
        };

        using var reader = new StreamReader(csv);
        using var csvReader = new CsvReader(reader, config);

        csvReader.Read();
        csvReader.ReadHeader();
        var headers = csvReader.HeaderRecord ?? Array.Empty<string>();

        string? Col(params string[] nomes) =>
            headers.FirstOrDefault(h => nomes.Any(n => h.Trim().Equals(n, StringComparison.OrdinalIgnoreCase)));

        var colId        = Col("id", "ticket_id", "ticketid", "número", "numero");
        var colAssunto   = Col("subject", "assunto", "title", "título", "titulo");
        var colDescricao = Col("description", "descricao", "descrição", "body", "message", "mensagem", "texto", "text");

        var itens = new List<TicketCsvInput>();
        while (csvReader.Read())
        {
            var assunto   = colAssunto   != null ? csvReader.GetField(colAssunto)   : null;
            var descricao = colDescricao != null ? csvReader.GetField(colDescricao) : null;

            if (string.IsNullOrWhiteSpace(descricao))
                descricao = string.Join(" ", headers.Select(h => csvReader.GetField(h)).Where(v => !string.IsNullOrWhiteSpace(v)));

            if (string.IsNullOrWhiteSpace(assunto) && string.IsNullOrWhiteSpace(descricao))
                continue;

            itens.Add(new TicketCsvInput
            {
                ExternalId = colId != null ? csvReader.GetField(colId) : null,
                Assunto = assunto,
                Descricao = descricao ?? "",
            });
        }
        return itens;
    }

    public byte[] Export(IEnumerable<Ticket> tickets, IReadOnlyList<string>? colunas = null)
    {
        var cols = colunas?.Where(c => TodasAsChaves.Contains(c)).ToList();
        if (cols is null || cols.Count == 0)
            cols = ColunasDisponiveis.Select(c => c.Key).ToList();

        var labelMap = ColunasDisponiveis.ToDictionary(c => c.Key, c => c.Label, StringComparer.OrdinalIgnoreCase);

        using var ms = new MemoryStream();
        using (var writer = new StreamWriter(ms, new UTF8Encoding(true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var col in cols)
                csv.WriteField(labelMap[col]);
            csv.NextRecord();

            foreach (var t in tickets)
            {
                foreach (var col in cols)
                    csv.WriteField(GetValue(t, col));
                csv.NextRecord();
            }
        }
        return ms.ToArray();
    }

    private static string GetValue(Ticket t, string col) => col.ToLowerInvariant() switch
    {
        "id"              => t.ExternalId ?? "",
        "assunto"         => t.Assunto ?? "",
        "descricao"       => t.Descricao,
        "categoria"       => t.Categoria,
        "prioridade"      => t.Prioridade,
        "departamento"    => t.Departamento,
        "sentimento"      => t.Sentimento,
        "tags"            => t.Tags,
        "resumo"          => t.Resumo,
        "confianca"       => t.Confianca.ToString("0.00", CultureInfo.InvariantCulture),
        "justificativa"   => t.Justificativa,
        "modificado"      => t.RegistroModificado ? "Sim" : "Não",
        "datamodificacao" => t.DataModificacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
        _                 => "",
    };
}
