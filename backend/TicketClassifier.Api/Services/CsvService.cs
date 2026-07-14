using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TicketClassifier.Api.Dtos.Input;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Services;

public class CsvValidationException : Exception
{
    public string Codigo { get; }
    public CsvValidationException(string codigo, string mensagem) : base(mensagem) => Codigo = codigo;
}

/// <summary>Leitura e geração de CSV (parse flexível + export enriquecido).</summary>
public class CsvService
{
    public const int MaxTamanhoBytes = 50 * 1024 * 1024; // 50 MB
    public const int MaxLinhas = 100_000;
    public const int MaxColunas = 100;
    public const int MaxTamanhoCampo = 50_000;

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
        ValidarStream(csv);

        var ms = CopiarComLimite(csv);
        var bytes = ms.ToArray();

        ValidarConteudoBinario(bytes);
        var texto = DecodificarTexto(bytes);
        ValidarConteudoTexto(texto);

        using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(texto));
        return ParseInterno(textStream);
    }

    private static void ValidarStream(Stream csv)
    {
        if (csv is null || !csv.CanRead)
            throw new CsvValidationException("STREAM_INVALIDO", "O arquivo não pôde ser lido. Verifique se o arquivo não está corrompido.");
    }

    private static MemoryStream CopiarComLimite(Stream csv)
    {
        var ms = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;
        int lido;
        while ((lido = csv.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += lido;
            if (total > MaxTamanhoBytes)
                throw new CsvValidationException("ARQUIVO_GRANDE", $"O arquivo excede o limite de {MaxTamanhoBytes / (1024 * 1024)} MB.");
            ms.Write(buffer, 0, lido);
        }
        if (total == 0)
            throw new CsvValidationException("ARQUIVO_VAZIO", "O arquivo está vazio.");
        ms.Position = 0;
        return ms;
    }

    internal static void ValidarConteudoBinario(byte[] bytes)
    {
        // UTF-16 com BOM tem bytes nulos intercalados — não é binário
        if (bytes.Length >= 2 &&
            ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)))
            return;

        var amostra = Math.Min(bytes.Length, 8192);
        int nulos = 0;
        for (var i = 0; i < amostra; i++)
        {
            if (bytes[i] == 0x00) nulos++;
        }
        if (nulos > amostra * 0.05)
            throw new CsvValidationException("ARQUIVO_BINARIO", "O arquivo parece ser binário (PDF, imagem, etc.), não um CSV de texto.");
    }

    internal static string DecodificarTexto(byte[] bytes)
    {
        // BOM UTF-8
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        // BOM UTF-16 LE
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        // BOM UTF-16 BE
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        // Tenta UTF-8 primeiro; se falhar, usa Latin1
        try
        {
            var utf8 = new UTF8Encoding(false, throwOnInvalidBytes: true);
            return utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    internal static void ValidarConteudoTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new CsvValidationException("ARQUIVO_VAZIO", "O arquivo está vazio ou contém apenas espaços em branco.");

        var primeiraLinha = texto.AsSpan();
        var fimLinha = texto.IndexOfAny(new[] { '\r', '\n' });
        if (fimLinha > 0) primeiraLinha = texto.AsSpan(0, fimLinha);

        if (primeiraLinha.Length > 0 && (primeiraLinha[0] == '{' || primeiraLinha[0] == '['))
            throw new CsvValidationException("FORMATO_JSON", "O arquivo parece estar em formato JSON, não CSV.");

        if (primeiraLinha.StartsWith("<?xml") || primeiraLinha.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            throw new CsvValidationException("FORMATO_XML_HTML", "O arquivo parece estar em formato XML/HTML, não CSV.");

        if (primeiraLinha.StartsWith("%PDF"))
            throw new CsvValidationException("FORMATO_PDF", "O arquivo é um PDF, não um CSV.");
    }

    private List<TicketCsvInput> ParseInterno(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            DetectDelimiter = true,
            BadDataFound = null,
        };

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        using var csvReader = new CsvReader(reader, config);

        if (!csvReader.Read() || !csvReader.ReadHeader())
            throw new CsvValidationException("SEM_CABECALHO", "O arquivo não possui linha de cabeçalho válida.");

        var headers = csvReader.HeaderRecord ?? Array.Empty<string>();

        if (headers.Length == 0)
            throw new CsvValidationException("SEM_CABECALHO", "Não foi possível identificar colunas no cabeçalho.");

        if (headers.Length > MaxColunas)
            throw new CsvValidationException("EXCESSO_COLUNAS", $"O arquivo possui {headers.Length} colunas, o limite é {MaxColunas}.");

        // Mapear headers originais para versões limpas (para matching)
        var headersOriginais = headers.ToArray();
        var headersLimpos = headers.Select(LimparTexto).ToArray();

        string? Col(params string[] nomes)
        {
            for (var i = 0; i < headersLimpos.Length; i++)
                if (nomes.Any(n => headersLimpos[i].Equals(n, StringComparison.OrdinalIgnoreCase)))
                    return headersOriginais[i];
            return null;
        }

        var colId        = Col("id", "ticket_id", "ticketid", "número", "numero");
        var colAssunto   = Col("subject", "assunto", "title", "título", "titulo");
        var colDescricao = Col("description", "descricao", "descrição", "body", "message", "mensagem", "texto", "text");

        var itens = new List<TicketCsvInput>();
        var linhasIgnoradas = 0;
        var linha = 1;

        while (csvReader.Read())
        {
            linha++;
            if (linha > MaxLinhas + 1)
                throw new CsvValidationException("EXCESSO_LINHAS", $"O arquivo excede o limite de {MaxLinhas:N0} linhas.");

            string? assunto = null;
            string? descricao = null;
            string? externalId = null;

            try
            {
                assunto = colAssunto != null ? LimparCampo(csvReader.GetField(colAssunto)) : null;
                descricao = colDescricao != null ? LimparCampo(csvReader.GetField(colDescricao)) : null;
                externalId = colId != null ? csvReader.GetField(colId)?.Trim() : null;
            }
            catch (CsvHelper.MissingFieldException)
            {
                linhasIgnoradas++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(descricao))
            {
                var partes = new List<string>();
                foreach (var h in headersOriginais)
                {
                    try
                    {
                        var v = csvReader.GetField(h);
                        if (!string.IsNullOrWhiteSpace(v)) partes.Add(v.Trim());
                    }
                    catch { /* campo ausente nesta linha */ }
                }
                descricao = string.Join(" ", partes);
            }

            if (string.IsNullOrWhiteSpace(assunto) && string.IsNullOrWhiteSpace(descricao))
            {
                linhasIgnoradas++;
                continue;
            }

            if (descricao?.Length > MaxTamanhoCampo)
                descricao = descricao[..MaxTamanhoCampo];
            if (assunto?.Length > MaxTamanhoCampo)
                assunto = assunto[..MaxTamanhoCampo];

            itens.Add(new TicketCsvInput
            {
                ExternalId = externalId,
                Assunto = assunto,
                Descricao = descricao ?? "",
            });
        }

        return itens;
    }

    internal static string LimparTexto(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        var sb = new StringBuilder(texto.Length);
        foreach (var ch in texto)
        {
            if (ch == '﻿' || ch == '​' || ch == '‌' || ch == '‍' || ch == '￾')
                continue;
            sb.Append(ch);
        }
        return sb.ToString().Trim();
    }

    internal static string? LimparCampo(string? valor)
    {
        if (valor is null) return null;
        var limpo = valor
            .Replace("\0", "")
            .Replace("﻿", "")
            .Replace("�", "");
        return limpo.Trim();
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
