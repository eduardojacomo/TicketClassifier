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
        IReadOnlyList<TicketParaClassificar> itens, CancellationToken ct = default)
    {
        if (itens.Count == 0) return Array.Empty<ClassificacaoResultado>();

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var payloadJson = JsonSerializer.Serialize(new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = ClassificacaoPromptBuilder.ConstruirLote(itens) } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = Math.Min(8192, 200 + itens.Count * 120)
            }
        });

        var texto = await ChamarComRetryAsync(url, payloadJson, ct);
        var porIndice = texto is null
            ? new Dictionary<int, ClassificacaoResultado>()
            : SeguroParse(texto);

        // Alinha à ordem de entrada; faltantes recebem fallback.
        return itens.Select(t => porIndice.GetValueOrDefault(t.Indice, Categorias.Fallback)).ToList();
    }

    private async Task<string?> ChamarComRetryAsync(string url, string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 1000, 3000, 8000 };
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
                    _logger.LogWarning("Gemini {Status} (rate limit?), retry {N} em {Ms}ms.", (int)resp.StatusCode, tentativa + 1, backoff[tentativa]);
                    await Task.Delay(backoff[tentativa], ct);
                    continue;
                }
                resp.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();
            }
            catch (Exception ex) when (tentativa < backoff.Length)
            {
                _logger.LogWarning(ex, "Erro no Gemini, retry {N} em {Ms}ms.", tentativa + 1, backoff[tentativa]);
                await Task.Delay(backoff[tentativa], ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha final no Gemini; lote cai no fallback.");
                return null;
            }
        }
    }

    private Dictionary<int, ClassificacaoResultado> SeguroParse(string texto)
    {
        try { return Categorias.ParseLote(texto); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao parsear o array do Gemini; lote cai no fallback.");
            return new();
        }
    }

    private static bool EhTransiente(System.Net.HttpStatusCode s)
        => (int)s == 429 || (int)s == 500 || (int)s == 502 || (int)s == 503 || (int)s == 504;
}
