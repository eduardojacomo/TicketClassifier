using System.Text;
using System.Text.Json;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways;

/// <summary>Estratégia via Google Generative Language API (Gemini), em lote.</summary>
public class GeminiGateway : IClassificacaoGateway
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiGateway> _logger;

    public string Nome => "gemini";

    public GeminiGateway(HttpClient http, IConfiguration cfg, ILogger<GeminiGateway> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = cfg["Llm:Gemini:ApiKey"] ?? "";
        _model = cfg["Llm:Gemini:Model"] ?? "gemini-2.5-flash";
    }

    public async Task<IReadOnlyList<ClassificacaoResultado>> ClassificarLoteAsync(
        IReadOnlyList<TicketParaClassificar> itens, CancellationToken ct = default,
        int loteAtual = 1, int totalLotes = 1, int totalTickets = 0)
    {
        if (itens.Count == 0) return Array.Empty<ClassificacaoResultado>();

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var payloadJson = JsonSerializer.Serialize(new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = ClassificacaoPromptBuilder.ConstruirLote(itens, loteAtual, totalLotes, totalTickets) } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = Math.Max(4096, 500 + itens.Count * 400),
                thinkingConfig = new { thinkingBudget = 1024 }
            }
        });

        var (texto, erro) = await ChamarComRetryAsync(url, payloadJson, ct);

        if (texto is null)
        {
            var fallback = Categorias.FallbackComErro($"[Gemini] {erro ?? "Erro desconhecido"}");
            return itens.Select(_ => fallback).ToList();
        }

        _logger.LogInformation("Gemini resposta bruta ({Len} chars): {Texto}", texto.Length, texto.Length > 1000 ? texto[..1000] + "…" : texto);

        var indices = itens.Select(t => t.Indice).ToList();
        var (porIndice, parseErro) = SeguroParse(texto, indices);
        return itens.Select(t =>
        {
            if (porIndice.TryGetValue(t.Indice, out var r)) return r;
            var motivo = parseErro ?? $"[Gemini] Índice {t.Indice} ausente na resposta.";
            return Categorias.FallbackComErro(motivo);
        }).ToList();
    }

    private async Task<(string? texto, string? erro)> ChamarComRetryAsync(string url, string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 1000, 3000, 8000 };
        string? ultimoErro = null;

        for (var tentativa = 0; ; tentativa++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };
                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (EhTransiente(resp.StatusCode) && tentativa < backoff.Length)
                {
                    ultimoErro = $"HTTP {(int)resp.StatusCode}: {Truncar(body, 300)}";
                    _logger.LogWarning("Gemini {Status}, retry {N} em {Ms}ms. Body: {Body}", (int)resp.StatusCode, tentativa + 1, backoff[tentativa], Truncar(body, 200));
                    await Task.Delay(backoff[tentativa], ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    ultimoErro = $"HTTP {(int)resp.StatusCode}: {Truncar(body, 300)}";
                    _logger.LogWarning("Gemini resposta não-OK: {Status}. Body: {Body}", (int)resp.StatusCode, Truncar(body, 500));
                    return (null, ultimoErro);
                }

                using var doc = JsonDocument.Parse(body);
                var parts = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts");

                string? textoSaida = null;
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                        continue;
                    if (part.TryGetProperty("text", out var textEl))
                        textoSaida = textEl.GetString();
                }
                textoSaida ??= parts[parts.GetArrayLength() - 1].GetProperty("text").GetString();
                return (textoSaida, null);
            }
            catch (Exception ex) when (tentativa < backoff.Length)
            {
                ultimoErro = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Erro no Gemini, retry {N} em {Ms}ms.", tentativa + 1, backoff[tentativa]);
                await Task.Delay(backoff[tentativa], ct);
            }
            catch (Exception ex)
            {
                ultimoErro = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Falha final no Gemini; lote cai no fallback.");
                return (null, ultimoErro);
            }
        }
    }

    private (Dictionary<int, ClassificacaoResultado> resultado, string? erro) SeguroParse(string texto, IReadOnlyList<int> indices)
    {
        try { return (Categorias.ParseLoteComFallback(texto, indices), null); }
        catch (Exception ex) { return (new(), $"Falha ao parsear resposta: {ex.Message}. Resposta: {Truncar(texto, 300)}"); }
    }

    private static string Truncar(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    private static bool EhTransiente(System.Net.HttpStatusCode s)
        => (int)s == 429 || (int)s == 500 || (int)s == 502 || (int)s == 503 || (int)s == 504;
}
