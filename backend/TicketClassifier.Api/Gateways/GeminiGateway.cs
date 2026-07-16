using System.Text;
using System.Text.Json;
using TicketClassifier.Api.Gateways.Interface;
using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways;

/// <summary>Strategy via the Google Generative Language API (Gemini), in batches.</summary>
public class GeminiGateway : IClassificationGateway
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GeminiGateway> _logger;

    public string Name => "gemini";

    public GeminiGateway(HttpClient http, IConfiguration cfg, ILogger<GeminiGateway> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = cfg["Llm:Gemini:ApiKey"] ?? "";
        _model = cfg["Llm:Gemini:Model"] ?? "gemini-2.5-flash";
    }

    public async Task<IReadOnlyList<ClassificationResult>> ClassifyBatchAsync(
        IReadOnlyList<TicketToClassify> items, ClassificationPromptBuilder promptBuilder,
        CancellationToken ct = default,
        int currentBatch = 1, int totalBatches = 1, int totalTickets = 0)
    {
        if (items.Count == 0) return Array.Empty<ClassificationResult>();

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var payloadJson = JsonSerializer.Serialize(new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = promptBuilder.BuildBatch(items, currentBatch, totalBatches, totalTickets) } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = Math.Max(4096, 500 + items.Count * 400),
                thinkingConfig = new { thinkingBudget = 1024 }
            }
        });

        var (text, error) = await CallWithRetryAsync(url, payloadJson, ct);

        if (text is null)
        {
            var fallback = Categories.FallbackWithError($"[Gemini] {error ?? "Unknown error"}");
            return items.Select(_ => fallback).ToList();
        }

        _logger.LogInformation("Gemini raw response ({Len} chars): {Text}", text.Length, text.Length > 1000 ? text[..1000] + "..." : text);

        var indices = items.Select(t => t.Index).ToList();
        var (byIndex, parseError) = SafeParse(text, indices);
        return items.Select(t =>
        {
            if (byIndex.TryGetValue(t.Index, out var r)) return r;
            var reason = parseError ?? $"[Gemini] Index {t.Index} missing in response.";
            return Categories.FallbackWithError(reason);
        }).ToList();
    }

    private async Task<(string? text, string? error)> CallWithRetryAsync(string url, string payloadJson, CancellationToken ct)
    {
        var backoff = new[] { 1000, 3000, 8000 };
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
                    _logger.LogWarning("Gemini {Status}, retry {N} in {Ms}ms. Body: {Body}", (int)resp.StatusCode, attempt + 1, backoff[attempt], Truncate(body, 200));
                    await Task.Delay(backoff[attempt], ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 300)}";
                    _logger.LogWarning("Gemini non-OK response: {Status}. Body: {Body}", (int)resp.StatusCode, Truncate(body, 500));
                    return (null, lastError);
                }

                using var doc = JsonDocument.Parse(body);
                var parts = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts");

                string? outputText = null;
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                        continue;
                    if (part.TryGetProperty("text", out var textEl))
                        outputText = textEl.GetString();
                }
                outputText ??= parts[parts.GetArrayLength() - 1].GetProperty("text").GetString();
                return (outputText, null);
            }
            catch (Exception ex) when (attempt < backoff.Length)
            {
                lastError = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Gemini error, retry {N} in {Ms}ms.", attempt + 1, backoff[attempt]);
                await Task.Delay(backoff[attempt], ct);
            }
            catch (Exception ex)
            {
                lastError = $"{ex.GetType().Name}: {ex.Message}";
                _logger.LogWarning(ex, "Final failure in Gemini; batch falls back to default.");
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
