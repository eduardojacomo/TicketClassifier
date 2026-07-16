using System.Text;
using System.Text.Json;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways;

/// <summary>Strategy via a llama.cpp server (OpenAI-compatible API).</summary>
public class LlamaGateway : IClassificationGateway
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly ILogger<LlamaGateway> _logger;

    public string Name => "llama";

    public LlamaGateway(HttpClient http, IConfiguration cfg, ILogger<LlamaGateway> logger)
    {
        _http = http;
        _logger = logger;
        _baseUrl = cfg["Llm:Llama:BaseUrl"]?.TrimEnd('/') ?? "http://llama-server:8080";
    }

    public async Task<IReadOnlyList<ClassificationResult>> ClassifyBatchAsync(
        IReadOnlyList<TicketToClassify> items, ClassificationPromptBuilder promptBuilder,
        CancellationToken ct = default,
        int currentBatch = 1, int totalBatches = 1, int totalTickets = 0)
    {
        if (items.Count == 0) return Array.Empty<ClassificationResult>();

        var url = $"{_baseUrl}/v1/chat/completions";
        var payloadJson = JsonSerializer.Serialize(new
        {
            messages = new[]
            {
                new { role = "user", content = promptBuilder.BuildBatch(items, currentBatch, totalBatches, totalTickets) }
            },
            temperature = 0.2,
            max_tokens = Math.Max(4096, 500 + items.Count * 400)
        });

        var (text, error) = await CallWithRetryAsync(url, payloadJson, ct);

        if (text is null)
        {
            var fallback = Categories.FallbackWithError($"[Llama] {error ?? "Unknown error"}");
            return items.Select(_ => fallback).ToList();
        }

        _logger.LogInformation("Llama raw response ({Len} chars): {Text}", text.Length, text.Length > 1000 ? text[..1000] + "..." : text);

        var indices = items.Select(t => t.Index).ToList();
        var (byIndex, parseError) = SafeParse(text, indices);

        return items.Select(t =>
        {
            if (byIndex.TryGetValue(t.Index, out var r)) return r;
            var reason = parseError ?? $"[Llama] Index {t.Index} missing in response. Partial response: {Truncate(text, 200)}";
            return Categories.FallbackWithError(reason);
        }).ToList();
    }

    private async Task<(string? text, string? error)> CallWithRetryAsync(string url, string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 2000, 5000, 10000 };
        string? lastError = null;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
                };
                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (IsTransient(resp.StatusCode) && attempt < backoff.Length)
                {
                    lastError = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 300)}";
                    _logger.LogWarning("Llama {Status}, retry {N} in {Ms}ms. Body: {Body}", (int)resp.StatusCode, attempt + 1, backoff[attempt], Truncate(body, 200));
                    await Task.Delay(backoff[attempt], ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 300)}";
                    _logger.LogWarning("Llama non-OK response: {Status}. Body: {Body}", (int)resp.StatusCode, Truncate(body, 500));
                    return (null, lastError);
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
                lastError = $"Timeout after {_http.Timeout.TotalSeconds}s waiting for Llama response";
                if (attempt < backoff.Length)
                {
                    _logger.LogWarning("Llama timeout, retry {N} in {Ms}ms.", attempt + 1, backoff[attempt]);
                    await Task.Delay(backoff[attempt], ct);
                    continue;
                }
                _logger.LogWarning("Llama final timeout; batch falls back to default.");
                return (null, lastError);
            }
            catch (Exception ex) when (attempt < backoff.Length)
            {
                lastError = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Llama error, retry {N} in {Ms}ms.", attempt + 1, backoff[attempt]);
                await Task.Delay(backoff[attempt], ct);
            }
            catch (Exception ex)
            {
                lastError = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Final failure in Llama; batch falls back to default.");
                return (null, lastError);
            }
        }
    }

    private (Dictionary<int, ClassificationResult> result, string? error) SafeParse(string text, IReadOnlyList<int> indices)
    {
        try
        {
            var parsed = Categories.ParseBatchWithFallback(text, indices);
            return (parsed, null);
        }
        catch (Exception ex)
        {
            var error = $"Failed to parse Llama response: {ex.Message}. Response: {Truncate(text, 300)}";
            _logger.LogWarning(ex, "Failed to parse Llama response. Text: {Text}", Truncate(text, 500));
            return (new(), error);
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    private static bool IsTransient(System.Net.HttpStatusCode s)
        => (int)s == 429 || (int)s == 500 || (int)s == 502 || (int)s == 503 || (int)s == 504;
}
