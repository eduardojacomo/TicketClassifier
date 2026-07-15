using TicketClassifier.Api.Gateways.Interface;

namespace TicketClassifier.Api.Gateways;

/// <summary>
/// Seleciona a estratégia de classificação com base em Llm:Provider
/// (anthropic | gemini | llama).
/// </summary>
public class ClassificacaoGatewayFactory : IClassificacaoGatewayFactory
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;

    public ClassificacaoGatewayFactory(IServiceProvider sp, IConfiguration cfg)
    {
        _sp = sp;
        _cfg = cfg;
    }

    public IClassificacaoGateway Criar()
    {
        var provider = _cfg["Llm:Provider"]?.Trim().ToLowerInvariant();

        return provider switch
        {
            "anthropic" when TemChave("Llm:Anthropic:ApiKey")
                => _sp.GetRequiredService<AnthropicGateway>(),
            "gemini" when TemChave("Llm:Gemini:ApiKey")
                => _sp.GetRequiredService<GeminiGateway>(),
            "llama" => _sp.GetRequiredService<LlamaGateway>(),
            _ => throw new InvalidOperationException(
                $"No valid LLM provider configured. Set 'Llm:Provider' to 'anthropic', 'gemini', or 'llama' and provide the corresponding API key."),
        };
    }

    private bool TemChave(string key) => !string.IsNullOrWhiteSpace(_cfg[key]);
}
