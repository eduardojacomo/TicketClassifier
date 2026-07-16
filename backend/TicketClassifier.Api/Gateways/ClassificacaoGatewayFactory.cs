using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Api.Gateways;

/// <summary>
/// Selects the classification strategy based on Llm:Provider
/// (anthropic | gemini | llama).
/// </summary>
public class ClassificationGatewayFactory : IClassificationGatewayFactory
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;

    public ClassificationGatewayFactory(IServiceProvider sp, IConfiguration cfg)
    {
        _sp = sp;
        _cfg = cfg;
    }

    public IClassificationGateway Create()
    {
        var provider = _cfg["Llm:Provider"]?.Trim().ToLowerInvariant();

        return provider switch
        {
            "anthropic" when HasKey("Llm:Anthropic:ApiKey")
                => _sp.GetRequiredService<AnthropicGateway>(),
            "gemini" when HasKey("Llm:Gemini:ApiKey")
                => _sp.GetRequiredService<GeminiGateway>(),
            "llama" => _sp.GetRequiredService<LlamaGateway>(),
            _ => throw new InvalidOperationException(
                $"No valid LLM provider configured. Set 'Llm:Provider' to 'anthropic', 'gemini', or 'llama' and provide the corresponding API key."),
        };
    }

    private bool HasKey(string key) => !string.IsNullOrWhiteSpace(_cfg[key]);
}
