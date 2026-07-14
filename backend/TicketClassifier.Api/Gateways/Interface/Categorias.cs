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
        { "Login", "Pagamento", "Bug", "Performance", "Integração", "Cadastro", "Financeiro", "Comercial", "Sugestão", "Outro" };

    public static readonly string[] Prioridades =
        { "Baixa", "Média", "Alta", "Crítica" };

    public static readonly string[] Departamentos =
        { "Suporte", "Financeiro", "Comercial", "Produto", "Desenvolvimento" };

    public static readonly ClassificacaoResultado Fallback =
        new("Outro", "Média", "Suporte", "", 0.0, "Não classificado.");

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

    /// <summary>Converte a resposta (array JSON) em dicionário indice → resultado.</summary>
    public static Dictionary<int, ClassificacaoResultado> ParseLote(string textoModelo)
    {
        var i = textoModelo.IndexOf('[');
        var j = textoModelo.LastIndexOf(']');
        var json = (i >= 0 && j > i) ? textoModelo[i..(j + 1)] : "[]";

        var resultado = new Dictionary<int, ClassificacaoResultado>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return resultado;

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (!el.TryGetProperty("indice", out var idxEl) || !idxEl.TryGetInt32(out var idx))
                continue;

            string? S(string p) => el.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            double D(string p) => el.TryGetProperty(p, out var v) && v.TryGetDouble(out var d) ? d : 0.8;

            resultado[idx] = new ClassificacaoResultado(
                CategoriaValida(S("categoria")),
                PrioridadeValida(S("prioridade")),
                DepartamentoValido(S("departamento")),
                S("resumo") ?? "",
                Math.Clamp(D("confianca"), 0, 1),
                S("justificativa") ?? "");
        }
        return resultado;
    }
}
