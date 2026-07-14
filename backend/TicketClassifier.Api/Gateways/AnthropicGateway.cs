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
        IReadOnlyList<TicketParaClassificar> itens, ClassificacaoPromptBuilder promptBuilder,
        CancellationToken ct = default,
        int loteAtual = 1, int totalLotes = 1, int totalTickets = 0)
    {
        if (itens.Count == 0) return Array.Empty<ClassificacaoResultado>();

        var payloadJson = JsonSerializer.Serialize(new
        {
            model = _model,
            max_tokens = Math.Max(4096, 500 + itens.Count * 400),
            messages = new[] { new { role = "user", content = promptBuilder.ConstruirLote(itens, loteAtual, totalLotes, totalTickets) } }
        });

        var (texto, erro) = await ChamarComRetryAsync(payloadJson, ct);

        if (texto is null)
        {
            var fallback = Categorias.FallbackComErro($"[Anthropic] {erro ?? "Erro desconhecido"}");
            return itens.Select(_ => fallback).ToList();
        }

        _logger.LogInformation("Anthropic resposta bruta ({Len} chars): {Texto}", texto.Length, texto.Length > 1000 ? texto[..1000] + "…" : texto);

        var indices = itens.Select(t => t.Indice).ToList();
        var (porIndice, parseErro) = SeguroParse(texto, indices);
        return itens.Select(t =>
        {
            if (porIndice.TryGetValue(t.Indice, out var r)) return r;
            var motivo = parseErro ?? $"[Anthropic] Índice {t.Indice} ausente na resposta.";
            return Categorias.FallbackComErro(motivo);
        }).ToList();
    }

    private async Task<(string? texto, string? erro)> ChamarComRetryAsync(string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 1000, 3000, 8000 };
        string? ultimoErro = null;

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
                    ultimoErro = $"HTTP {(int)resp.StatusCode}: {Truncar(body, 300)}";
                    await Task.Delay(backoff[tentativa], ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    ultimoErro = $"HTTP {(int)resp.StatusCode}: {Truncar(body, 300)}";
                    _logger.LogWarning("Anthropic resposta não-OK: {Status}. Body: {Body}", (int)resp.StatusCode, Truncar(body, 500));
                    return (null, ultimoErro);
                }

                using var doc = JsonDocument.Parse(body);
                return (doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString(), null);
            }
            catch (Exception ex) when (tentativa < backoff.Length)
            {
                ultimoErro = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Erro no Anthropic, retry {N}.", tentativa + 1);
                await Task.Delay(backoff[tentativa], ct);
            }
            catch (Exception ex)
            {
                ultimoErro = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Falha final no Anthropic; lote cai no fallback.");
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
