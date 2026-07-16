using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using TicketClassifier.Api.Dtos.Input;
using TicketClassifier.Api.Models;

namespace TicketClassifier.Api.Services;

public class CsvValidationException : Exception
{
    public string Code { get; }
    public CsvValidationException(string code, string message) : base(message) => Code = code;
}

/// <summary>CSV reading and generation (flexible parsing + enriched export).</summary>
public class CsvService
{
    public const int MaxSizeBytes = 50 * 1024 * 1024; // 50 MB
    public const int MaxLines = 100_000;
    public const int MaxColumns = 100;
    public const int MaxFieldSize = 50_000;

    public static readonly (string Key, string Label)[] AvailableColumns =
    {
        ("id",              "ID"),
        ("assunto",         "Subject"),
        ("descricao",       "Description"),
        ("categoria",       "Category"),
        ("prioridade",      "Priority"),
        ("departamento",    "Department"),
        ("sentimento",      "Sentiment"),
        ("tags",            "Tags"),
        ("resumo",          "Summary"),
        ("confianca",       "Confidence"),
        ("justificativa",   "Justification"),
        ("modificado",      "Modified"),
        ("dataModificacao", "Modified Date"),
    };

    private static readonly HashSet<string> AllKeys =
        new(AvailableColumns.Select(c => c.Key), StringComparer.OrdinalIgnoreCase);

    public List<TicketCsvInput> Parse(Stream csv)
    {
        ValidateStream(csv);

        var ms = CopyWithLimit(csv);
        var bytes = ms.ToArray();

        ValidateBinaryContent(bytes);
        var text = DecodeText(bytes);
        ValidateTextContent(text);

        using var textStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return ParseInternal(textStream);
    }

    private static void ValidateStream(Stream csv)
    {
        if (csv is null || !csv.CanRead)
            throw new CsvValidationException("STREAM_INVALIDO", "The file could not be read. Check that the file is not corrupted.");
    }

    private static MemoryStream CopyWithLimit(Stream csv)
    {
        var ms = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;
        int read;
        while ((read = csv.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > MaxSizeBytes)
                throw new CsvValidationException("ARQUIVO_GRANDE", $"The file exceeds the {MaxSizeBytes / (1024 * 1024)} MB limit.");
            ms.Write(buffer, 0, read);
        }
        if (total == 0)
            throw new CsvValidationException("ARQUIVO_VAZIO", "The file is empty.");
        ms.Position = 0;
        return ms;
    }

    internal static void ValidateBinaryContent(byte[] bytes)
    {
        // UTF-16 with BOM has interleaved null bytes — not binary
        if (bytes.Length >= 2 &&
            ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF)))
            return;

        var sample = Math.Min(bytes.Length, 8192);
        int nullCount = 0;
        for (var i = 0; i < sample; i++)
        {
            if (bytes[i] == 0x00) nullCount++;
        }
        if (nullCount > sample * 0.05)
            throw new CsvValidationException("ARQUIVO_BINARIO", "The file appears to be binary (PDF, image, etc.), not a text CSV.");
    }

    internal static string DecodeText(byte[] bytes)
    {
        // UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        // UTF-16 LE BOM
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

        // UTF-16 BE BOM
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

        // Try UTF-8 first; if it fails, use Latin1
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

    internal static void ValidateTextContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new CsvValidationException("ARQUIVO_VAZIO", "The file is empty or contains only whitespace.");

        var firstLine = text.AsSpan();
        var lineEnd = text.IndexOfAny(new[] { '\r', '\n' });
        if (lineEnd > 0) firstLine = text.AsSpan(0, lineEnd);

        if (firstLine.Length > 0 && (firstLine[0] == '{' || firstLine[0] == '['))
            throw new CsvValidationException("FORMATO_JSON", "The file appears to be in JSON format, not CSV.");

        if (firstLine.StartsWith("<?xml") || firstLine.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            throw new CsvValidationException("FORMATO_XML_HTML", "The file appears to be in XML/HTML format, not CSV.");

        if (firstLine.StartsWith("%PDF"))
            throw new CsvValidationException("FORMATO_PDF", "The file is a PDF, not a CSV.");
    }

    private List<TicketCsvInput> ParseInternal(Stream stream)
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
            throw new CsvValidationException("SEM_CABECALHO", "The file does not have a valid header row.");

        var headers = csvReader.HeaderRecord ?? Array.Empty<string>();

        if (headers.Length == 0)
            throw new CsvValidationException("SEM_CABECALHO", "Could not identify columns in the header.");

        if (headers.Length > MaxColumns)
            throw new CsvValidationException("EXCESSO_COLUNAS", $"The file has {headers.Length} columns, the limit is {MaxColumns}.");

        // Map original headers to cleaned versions (for matching)
        var originalHeaders = headers.ToArray();
        var cleanedHeaders = headers.Select(CleanText).ToArray();

        string? Col(params string[] names)
        {
            for (var i = 0; i < cleanedHeaders.Length; i++)
                if (names.Any(n => cleanedHeaders[i].Equals(n, StringComparison.OrdinalIgnoreCase)))
                    return originalHeaders[i];
            return null;
        }

        var colId          = Col("id", "ticket_id", "ticketid", "número", "numero");
        var colSubject     = Col("subject", "assunto", "title", "título", "titulo");
        var colDescription = Col("description", "descricao", "descrição", "body", "message", "mensagem", "texto", "text");

        var items = new List<TicketCsvInput>();
        var skippedLines = 0;
        var lineNumber = 1;

        while (csvReader.Read())
        {
            lineNumber++;
            if (lineNumber > MaxLines + 1)
                throw new CsvValidationException("EXCESSO_LINHAS", $"The file exceeds the limit of {MaxLines:N0} rows.");

            string? subject = null;
            string? description = null;
            string? externalId = null;

            try
            {
                subject = colSubject != null ? CleanField(csvReader.GetField(colSubject)) : null;
                description = colDescription != null ? CleanField(csvReader.GetField(colDescription)) : null;
                externalId = colId != null ? csvReader.GetField(colId)?.Trim() : null;
            }
            catch (CsvHelper.MissingFieldException)
            {
                skippedLines++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                var parts = new List<string>();
                foreach (var h in originalHeaders)
                {
                    try
                    {
                        var v = csvReader.GetField(h);
                        if (!string.IsNullOrWhiteSpace(v)) parts.Add(v.Trim());
                    }
                    catch { /* field missing in this row */ }
                }
                description = string.Join(" ", parts);
            }

            if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(description))
            {
                skippedLines++;
                continue;
            }

            if (description?.Length > MaxFieldSize)
                description = description[..MaxFieldSize];
            if (subject?.Length > MaxFieldSize)
                subject = subject[..MaxFieldSize];

            items.Add(new TicketCsvInput
            {
                ExternalId = externalId,
                Subject = subject,
                Description = description ?? "",
            });
        }

        return items;
    }

    internal static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch == '﻿' || ch == '​' || ch == '‌' || ch == '‍' || ch == '￾')
                continue;
            sb.Append(ch);
        }
        return sb.ToString().Trim();
    }

    internal static string? CleanField(string? value)
    {
        if (value is null) return null;
        var cleaned = value
            .Replace("\0", "")
            .Replace("﻿", "")
            .Replace("�", "");
        return cleaned.Trim();
    }

    public byte[] Export(IEnumerable<Ticket> tickets, IReadOnlyList<string>? columns = null)
    {
        var cols = columns?.Where(c => AllKeys.Contains(c)).ToList();
        if (cols is null || cols.Count == 0)
            cols = AvailableColumns.Select(c => c.Key).ToList();

        var labelMap = AvailableColumns.ToDictionary(c => c.Key, c => c.Label, StringComparer.OrdinalIgnoreCase);

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
        "assunto"         => t.Subject ?? "",
        "descricao"       => t.Description,
        "categoria"       => t.Category,
        "prioridade"      => t.Priority,
        "departamento"    => t.Department,
        "sentimento"      => t.Sentiment,
        "tags"            => t.Tags,
        "resumo"          => t.Summary,
        "confianca"       => t.Confidence.ToString("0.00", CultureInfo.InvariantCulture),
        "justificativa"   => t.Justification,
        "modificado"      => t.RecordModified ? "Yes" : "No",
        "datamodificacao" => t.ModifiedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
        _                 => "",
    };
}
