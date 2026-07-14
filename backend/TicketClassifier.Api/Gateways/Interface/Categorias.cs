using System.Text;
using System.Text.Json;

namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>
/// Domínio da classificação: valores permitidos (enums), validação tolerante
/// (sem acento), resultado de fallback e parse da resposta do LLM (array JSON).
/// </summary>
public static class Categorias
{
    public static readonly string[] Lista =
        { "Dúvida", "Bug", "Reclamação", "Login", "Pagamento", "Financeiro", "Performance", "Integração", "Cadastro", "Comercial", "Sugestão", "Elogio", "Outro" };

    public static readonly string[] Prioridades =
        { "Baixa", "Média", "Alta", "Crítica" };

    public static readonly string[] Departamentos =
        { "Suporte", "Financeiro", "Comercial", "Produto", "Desenvolvimento" };

    public static readonly string[] Sentimentos =
        { "positivo", "negativo", "neutro" };

    public static readonly ClassificacaoResultado Fallback =
        new("Outro", "Média", "Suporte", "", 0.0, "Não classificado.", "neutro", Array.Empty<string>());

    public static ClassificacaoResultado FallbackComErro(string erro) =>
        new("Outro", "Média", "Suporte", "", 0.0, erro, "neutro", Array.Empty<string>());

    public static bool EhFallback(ClassificacaoResultado r) =>
        r.Confianca == 0.0 && r.Categoria == "Outro" && r.Departamento == "Suporte" && string.IsNullOrEmpty(r.Resumo);

    private static string Normalizar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString();
    }

    private static string CasarOu(string[] valores, string? valor, string padrao)
        => valores.FirstOrDefault(x => Normalizar(x) == Normalizar(valor)) ?? padrao;

    public static string CategoriaValida(string? c)    => CasarOu(Lista, c, "Outro");
    public static string PrioridadeValida(string? p)   => CasarOu(Prioridades, p, "Média");
    public static string DepartamentoValido(string? d) => CasarOu(Departamentos, d, "Suporte");
    public static string SentimentoValido(string? s) => CasarOu(Sentimentos, s, "neutro");

    private static string LimparMarkdown(string texto)
    {
        var limpo = texto.Trim();
        if (limpo.StartsWith("```"))
        {
            var fimPrimeiraLinha = limpo.IndexOf('\n');
            if (fimPrimeiraLinha > 0)
                limpo = limpo[(fimPrimeiraLinha + 1)..];
        }
        if (limpo.EndsWith("```"))
            limpo = limpo[..^3];
        return limpo.Trim();
    }

    private static string ExtrairJson(string texto)
    {
        var limpo = LimparMarkdown(texto);
        var i = limpo.IndexOf('[');
        var j = limpo.LastIndexOf(']');
        return (i >= 0 && j > i) ? limpo[i..(j + 1)] : "[]";
    }

    /// <summary>Converte a resposta (array JSON) em dicionário indice → resultado.</summary>
    public static Dictionary<int, ClassificacaoResultado> ParseLote(string textoModelo)
    {
        var json = ExtrairJson(textoModelo);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return new Dictionary<int, ClassificacaoResultado>();

        return ParseItens(doc.RootElement);
    }

    /// <summary>
    /// Parse com fallback: tenta primeiro por índice explícito. Se nenhum
    /// índice bater com os esperados, remapeia pela ordem posicional.
    /// </summary>
    public static Dictionary<int, ClassificacaoResultado> ParseLoteComFallback(
        string textoModelo, IReadOnlyList<int> indicesEsperados)
    {
        var json = ExtrairJson(textoModelo);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return new Dictionary<int, ClassificacaoResultado>();

        var porIndice = ParseItens(doc.RootElement);

        var encontrados = indicesEsperados.Count(idx => porIndice.ContainsKey(idx));
        if (encontrados > 0) return porIndice;

        var elementos = doc.RootElement.EnumerateArray().ToList();
        var resultado = new Dictionary<int, ClassificacaoResultado>();
        for (var pos = 0; pos < Math.Min(elementos.Count, indicesEsperados.Count); pos++)
        {
            var r = ParseElemento(elementos[pos]);
            if (r is not null)
                resultado[indicesEsperados[pos]] = r;
        }
        return resultado;
    }

    private static Dictionary<int, ClassificacaoResultado> ParseItens(JsonElement array)
    {
        var resultado = new Dictionary<int, ClassificacaoResultado>();
        foreach (var el in array.EnumerateArray())
        {
            if (!el.TryGetProperty("indice", out var idxEl) || !idxEl.TryGetInt32(out var idx))
                continue;
            var r = ParseElemento(el);
            if (r is not null) resultado[idx] = r;
        }
        return resultado;
    }

    private static ClassificacaoResultado? ParseElemento(JsonElement el)
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

        return new ClassificacaoResultado(
            CategoriaValida(S("categoria")),
            PrioridadeValida(S("prioridade")),
            DepartamentoValido(S("departamento")),
            S("resumo") ?? "",
            Math.Clamp(D("confianca"), 0, 1),
            S("justificativa") ?? "",
            SentimentoValido(S("sentimento")),
            tags);
    }
}
