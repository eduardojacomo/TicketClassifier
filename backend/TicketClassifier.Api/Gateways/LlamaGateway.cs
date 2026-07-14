using System.Text;
using System.Text.Json;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways;

/// <summary>Estratégia via llama.cpp server (API compatível com OpenAI).</summary>
public class LlamaGateway : IClassificacaoGateway
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly ILogger<LlamaGateway> _logger;

    public string Nome => "llama";

    public LlamaGateway(HttpClient http, IConfiguration cfg, ILogger<LlamaGateway> logger)
    {
        _http = http;
        _logger = logger;
        _baseUrl = cfg["Llm:Llama:BaseUrl"]?.TrimEnd('/') ?? "http://llama-server:8080";
    }

    public async Task<IReadOnlyList<ClassificacaoResultado>> ClassificarLoteAsync(
        IReadOnlyList<TicketParaClassificar> itens, CancellationToken ct = default)
    {
        if (itens.Count == 0) return Array.Empty<ClassificacaoResultado>();

        var url = $"{_baseUrl}/v1/chat/completions";
        var payloadJson = JsonSerializer.Serialize(new
        {
            messages = new[]
            {
                new { role = "user", content = ClassificacaoPromptBuilder.ConstruirLote(itens) }
            },
            temperature = 0.2,
            max_tokens = Math.Min(8192, 200 + itens.Count * 120)
        });

        var texto = await ChamarComRetryAsync(url, payloadJson, ct);
        var porIndice = texto is null
            ? new Dictionary<int, ClassificacaoResultado>()
            : SeguroParse(texto);

        return itens.Select(t => porIndice.GetValueOrDefault(t.Indice, Categorias.Fallback)).ToList();
    }

    private async Task<string?> ChamarComRetryAsync(string url, string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 2000, 5000, 10000 };
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
                    _logger.LogWarning("Llama {Status}, retry {N} em {Ms}ms.", (int)resp.StatusCode, tentativa + 1, backoff[tentativa]);
                    await Task.Delay(backoff[tentativa], ct);
                    continue;
                }
                resp.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(body);
                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content").GetString();
            }
            catch (Exception ex) when (tentativa < backoff.Length)
            {
                _logger.LogWarning(ex, "Erro no Llama, retry {N} em {Ms}ms.", tentativa + 1, backoff[tentativa]);
                await Task.Delay(backoff[tentativa], ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha final no Llama; lote cai no fallback.");
                return null;
            }
        }
    }

    private Dictionary<int, ClassificacaoResultado> SeguroParse(string texto)
    {
        try { return Categorias.ParseLote(texto); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao parsear resposta do Llama; lote cai no fallback.");
            return new();
        }
    }

    private static bool EhTransiente(System.Net.HttpStatusCode s)
        => (int)s == 429 || (int)s == 500 || (int)s == 502 || (int)s == 503 || (int)s == 504;
}
