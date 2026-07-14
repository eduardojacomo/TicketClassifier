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
        IReadOnlyList<TicketParaClassificar> itens, ClassificacaoPromptBuilder promptBuilder,
        CancellationToken ct = default,
        int loteAtual = 1, int totalLotes = 1, int totalTickets = 0)
    {
        if (itens.Count == 0) return Array.Empty<ClassificacaoResultado>();

        var url = $"{_baseUrl}/v1/chat/completions";
        var payloadJson = JsonSerializer.Serialize(new
        {
            messages = new[]
            {
                new { role = "user", content = promptBuilder.ConstruirLote(itens, loteAtual, totalLotes, totalTickets) }
            },
            temperature = 0.2,
            max_tokens = Math.Max(4096, 500 + itens.Count * 400)
        });

        var (texto, erro) = await ChamarComRetryAsync(url, payloadJson, ct);

        if (texto is null)
        {
            var fallback = Categorias.FallbackComErro($"[Llama] {erro ?? "Erro desconhecido"}");
            return itens.Select(_ => fallback).ToList();
        }

        _logger.LogInformation("Llama resposta bruta ({Len} chars): {Texto}", texto.Length, texto.Length > 1000 ? texto[..1000] + "…" : texto);

        var indices = itens.Select(t => t.Indice).ToList();
        var (porIndice, parseErro) = SeguroParse(texto, indices);

        return itens.Select(t =>
        {
            if (porIndice.TryGetValue(t.Indice, out var r)) return r;
            var motivo = parseErro ?? $"[Llama] Índice {t.Indice} ausente na resposta. Resposta parcial: {Truncar(texto, 200)}";
            return Categorias.FallbackComErro(motivo);
        }).ToList();
    }

    private async Task<(string? texto, string? erro)> ChamarComRetryAsync(string url, string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 2000, 5000, 10000 };
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
                    _logger.LogWarning("Llama {Status}, retry {N} em {Ms}ms. Body: {Body}", (int)resp.StatusCode, tentativa + 1, backoff[tentativa], Truncar(body, 200));
                    await Task.Delay(backoff[tentativa], ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    ultimoErro = $"HTTP {(int)resp.StatusCode}: {Truncar(body, 300)}";
                    _logger.LogWarning("Llama resposta não-OK: {Status}. Body: {Body}", (int)resp.StatusCode, Truncar(body, 500));
                    return (null, ultimoErro);
                }

                using var doc = JsonDocument.Parse(body);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content").GetString();

                return (content, null);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                ultimoErro = $"Timeout após {_http.Timeout.TotalSeconds}s aguardando resposta do Llama";
                if (tentativa < backoff.Length)
                {
                    _logger.LogWarning("Llama timeout, retry {N} em {Ms}ms.", tentativa + 1, backoff[tentativa]);
                    await Task.Delay(backoff[tentativa], ct);
                    continue;
                }
                _logger.LogWarning("Llama timeout final; lote cai no fallback.");
                return (null, ultimoErro);
            }
            catch (Exception ex) when (tentativa < backoff.Length)
            {
                ultimoErro = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Erro no Llama, retry {N} em {Ms}ms.", tentativa + 1, backoff[tentativa]);
                await Task.Delay(backoff[tentativa], ct);
            }
            catch (Exception ex)
            {
                ultimoErro = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Falha final no Llama; lote cai no fallback.");
                return (null, ultimoErro);
            }
        }
    }

    private (Dictionary<int, ClassificacaoResultado> resultado, string? erro) SeguroParse(string texto, IReadOnlyList<int> indices)
    {
        try
        {
            var parsed = Categorias.ParseLoteComFallback(texto, indices);
            return (parsed, null);
        }
        catch (Exception ex)
        {
            var erro = $"Falha ao parsear resposta do Llama: {ex.Message}. Resposta: {Truncar(texto, 300)}";
            _logger.LogWarning(ex, "Falha ao parsear resposta do Llama. Texto: {Texto}", Truncar(texto, 500));
            return (new(), erro);
        }
    }

    private static string Truncar(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    private static bool EhTransiente(System.Net.HttpStatusCode s)
        => (int)s == 429 || (int)s == 500 || (int)s == 502 || (int)s == 503 || (int)s == 504;
}
