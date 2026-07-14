using System.Text;
using System.Text.Json;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways;

/// <summary>Estratégia via API de Mensagens da Anthropic (Claude), em lote.</summary>
public class AnthropicGateway : IClassificacaoGateway
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<AnthropicGateway> _logger;

    public string Nome => "anthropic";

    public AnthropicGateway(HttpClient http, IConfiguration cfg, ILogger<AnthropicGateway> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = cfg["Llm:Anthropic:ApiKey"] ?? "";
        _model = cfg["Llm:Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
    }

    public async Task<IReadOnlyList<ClassificacaoResultado>> ClassificarLoteAsync(
        IReadOnlyList<TicketParaClassificar> itens, CancellationToken ct = default)
    {
        if (itens.Count == 0) return Array.Empty<ClassificacaoResultado>();

        var payloadJson = JsonSerializer.Serialize(new
        {
            model = _model,
            max_tokens = Math.Min(8192, 200 + itens.Count * 120),
            messages = new[] { new { role = "user", content = ClassificacaoPromptBuilder.ConstruirLote(itens) } }
        });

        var texto = await ChamarComRetryAsync(payloadJson, ct);
        var porIndice = texto is null ? new() : SeguroParse(texto);
        return itens.Select(t => porIndice.GetValueOrDefault(t.Indice, Categorias.Fallback)).ToList();
    }

    private async Task<string?> ChamarComRetryAsync(string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 1000, 3000, 8000 };
        for (var tentativa = 0; ; tentativa++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                req.Headers.Add("x-api-key", _apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (EhTransiente(resp.StatusCode) && tentativa < backoff.Length)
                {
                    await Task.Delay(backoff[tentativa], ct);
                    continue;
                }
                resp.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            }
            catch (Exception ex) when (tentativa < backoff.Length)
            {
                _logger.LogWarning(ex, "Erro no Anthropic, retry {N}.", tentativa + 1);
                await Task.Delay(backoff[tentativa], ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha final no Anthropic; lote cai no fallback.");
                return null;
            }
        }
    }

    private Dictionary<int, ClassificacaoResultado> SeguroParse(string texto)
    {
        try { return Categorias.ParseLote(texto); }
        catch { return new(); }
    }

    private static bool EhTransiente(System.Net.HttpStatusCode s)
        => (int)s == 429 || (int)s == 500 || (int)s == 502 || (int)s == 503 || (int)s == 504;
}
