using System.Text;
using System.Text.Json;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways;

/// <summary>Strategy via the Anthropic (Claude) Messages API, in batches.</summary>
public class AnthropicGateway : IClassificationGateway
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<AnthropicGateway> _logger;

    public string Name => "anthropic";

    public AnthropicGateway(HttpClient http, IConfiguration cfg, ILogger<AnthropicGateway> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = cfg["Llm:Anthropic:ApiKey"] ?? "";
        _model = cfg["Llm:Anthropic:Model"] ?? "claude-haiku-4-5-20251001";
    }

    public async Task<IReadOnlyList<ClassificationResult>> ClassifyBatchAsync(
        IReadOnlyList<TicketToClassify> items, ClassificationPromptBuilder promptBuilder,
        CancellationToken ct = default,
        int currentBatch = 1, int totalBatches = 1, int totalTickets = 0)
    {
        if (items.Count == 0) return Array.Empty<ClassificationResult>();

        var payloadJson = JsonSerializer.Serialize(new
        {
            model = _model,
            max_tokens = Math.Max(4096, 500 + items.Count * 400),
            messages = new[] { new { role = "user", content = promptBuilder.BuildBatch(items, currentBatch, totalBatches, totalTickets) } }
        });

        var (text, error) = await CallWithRetryAsync(payloadJson, ct);

        if (text is null)
        {
            var fallback = Categories.FallbackWithError($"[Anthropic] {error ?? "Unknown error"}");
            return items.Select(_ => fallback).ToList();
        }

        _logger.LogInformation("Anthropic raw response ({Len} chars): {Text}", text.Length, text.Length > 1000 ? text[..1000] + "..." : text);

        var indices = items.Select(t => t.Index).ToList();
        var (byIndex, parseError) = SafeParse(text, indices);
        return items.Select(t =>
        {
            if (byIndex.TryGetValue(t.Index, out var r)) return r;
            var reason = parseError ?? $"[Anthropic] Index {t.Index} missing in response.";
            return Categories.FallbackWithError(reason);
        }).ToList();
    }

    private async Task<(string? text, string? error)> CallWithRetryAsync(string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 1000, 3000, 8000 };
        string? lastError = null;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                req.Headers.Add("x-api-key", _apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                req.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (IsTransient(resp.StatusCode) && attempt < backoff.Length)
                {
                    lastError = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 300)}";
                    await Task.Delay(backoff[attempt], ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 300)}";
                    _logger.LogWarning("Anthropic non-OK response: {Status}. Body: {Body}", (int)resp.StatusCode, Truncate(body, 500));
                    return (null, lastError);
                }

                using var doc = JsonDocument.Parse(body);
                return (doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString(), null);
            }
            catch (Exception ex) when (attempt < backoff.Length)
            {
                lastError = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Anthropic error, retry {N}.", attempt + 1);
                await Task.Delay(backoff[attempt], ct);
            }
            catch (Exception ex)
            {
                lastError = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Final failure in Anthropic; batch falls back to default.");
                return (null, lastError);
            }
        }
    }

    private (Dictionary<int, ClassificationResult> result, string? error) SafeParse(string text, IReadOnlyList<int> indices)
    {
        try { return (Categories.ParseBatchWithFallback(text, indices), null); }
        catch (Exception ex) { return (new(), $"Failed to parse response: {ex.Message}. Response: {Truncate(text, 300)}"); }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    private static bool IsTransient(System.Net.HttpStatusCode s)
        => (int)s == 429 || (int)s == 500 || (int)s == 502 || (int)s == 503 || (int)s == 504;
}
