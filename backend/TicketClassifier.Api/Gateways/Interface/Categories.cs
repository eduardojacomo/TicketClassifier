using System.Text;
using System.Text.Json;

namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>
/// Classification domain: allowed values (enums), tolerant validation
/// (accent-insensitive), fallback result, and parsing of the LLM response (JSON array).
/// </summary>
public static class Categories
{
    public static readonly string[] CategoryList =
        { "Question", "Bug", "Complaint", "Login", "Payment", "Financial", "Performance", "Integration", "Registration", "Sales", "Suggestion", "Praise", "Other" };

    public static readonly string[] PriorityList =
        { "Low", "Medium", "High", "Critical" };

    public static readonly string[] DepartmentList =
        { "Support", "Financial", "Sales", "Product", "Development" };

    public static readonly string[] SentimentList =
        { "positive", "negative", "neutral" };

    public static readonly ClassificationResult Fallback =
        new("Other", "Medium", "Support", "", 0.0, "Not classified.", "neutral", Array.Empty<string>());

    public static ClassificationResult FallbackWithError(string error) =>
        new("Other", "Medium", "Support", "", 0.0, error, "neutral", Array.Empty<string>());

    public static bool IsFallback(ClassificationResult r) =>
        r.Confidence == 0.0 && r.Category == "Other" && r.Department == "Support" && string.IsNullOrEmpty(r.Summary);

    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString();
    }

    private static string MatchOr(string[] values, string? value, string fallback)
        => values.FirstOrDefault(x => Normalize(x) == Normalize(value)) ?? fallback;

    public static string ValidCategory(string? c)    => MatchOr(CategoryList, c, "Other");
    public static string ValidPriority(string? p)    => MatchOr(PriorityList, p, "Medium");
    public static string ValidDepartment(string? d)  => MatchOr(DepartmentList, d, "Support");
    public static string ValidSentiment(string? s)   => MatchOr(SentimentList, s, "neutral");

    private static string CleanMarkdown(string text)
    {
        var cleaned = text.Trim();
        if (cleaned.StartsWith("```"))
        {
            var firstLineEnd = cleaned.IndexOf('\n');
            if (firstLineEnd > 0)
                cleaned = cleaned[(firstLineEnd + 1)..];
        }
        if (cleaned.EndsWith("```"))
            cleaned = cleaned[..^3];
        return cleaned.Trim();
    }

    private static string ExtractJson(string text)
    {
        var cleaned = CleanMarkdown(text);

        var arrayStart = cleaned.IndexOf('[');
        var objStart = cleaned.IndexOf('{');

        // If '[' comes before '{', it's an outer array
        if (arrayStart >= 0 && (objStart < 0 || arrayStart < objStart))
        {
            var arrayEnd = cleaned.LastIndexOf(']');
            if (arrayEnd > arrayStart)
                return cleaned[arrayStart..(arrayEnd + 1)];
        }

        // Single object — wrap in array
        if (objStart >= 0)
        {
            var objEnd = cleaned.LastIndexOf('}');
            if (objEnd > objStart)
                return "[" + cleaned[objStart..(objEnd + 1)] + "]";
        }

        return "[]";
    }

    /// <summary>Converts the response (JSON array) into a dictionary of index → result.</summary>
    public static Dictionary<int, ClassificationResult> ParseBatch(string modelText)
    {
        var json = ExtractJson(modelText);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return new Dictionary<int, ClassificationResult>();

        return ParseItems(doc.RootElement);
    }

    /// <summary>
    /// Parse with fallback: first tries by explicit index. If no
    /// index matches the expected ones, remaps by positional order.
    /// </summary>
    public static Dictionary<int, ClassificationResult> ParseBatchWithFallback(
        string modelText, IReadOnlyList<int> expectedIndices)
    {
        var json = ExtractJson(modelText);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return new Dictionary<int, ClassificationResult>();

        var byIndex = ParseItems(doc.RootElement);

        var foundCount = expectedIndices.Count(idx => byIndex.ContainsKey(idx));
        if (foundCount > 0) return byIndex;

        var elements = doc.RootElement.EnumerateArray().ToList();
        var result = new Dictionary<int, ClassificationResult>();
        for (var pos = 0; pos < Math.Min(elements.Count, expectedIndices.Count); pos++)
        {
            var r = ParseElement(elements[pos]);
            if (r is not null)
                result[expectedIndices[pos]] = r;
        }
        return result;
    }

    private static Dictionary<int, ClassificationResult> ParseItems(JsonElement array)
    {
        var result = new Dictionary<int, ClassificationResult>();
        foreach (var el in array.EnumerateArray())
        {
            if (!el.TryGetProperty("indice", out var idxEl) || !idxEl.TryGetInt32(out var idx))
                continue;
            var r = ParseElement(el);
            if (r is not null) result[idx] = r;
        }
        return result;
    }

    private static ClassificationResult? ParseElement(JsonElement el)
    {
        string? S(string p) => el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        double D(string p) => el.TryGetProperty(p, out var v) && v.TryGetDouble(out var d) ? d : 0.8;

        var tags = Array.Empty<string>();
        if (el.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            tags = tagsEl.EnumerateArray()
                .Where(t => t.ValueKind == JsonValueKind.String)
                .Select(t => t.GetString()!.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0)
                .Distinct()
                .ToArray();

        return new ClassificationResult(
            ValidCategory(S("categoria")),
            ValidPriority(S("prioridade")),
            ValidDepartment(S("departamento")),
            S("resumo") ?? "",
            Math.Clamp(D("confianca"), 0, 1),
            S("justificativa") ?? "",
            ValidSentiment(S("sentimento")),
            tags);
    }
}
