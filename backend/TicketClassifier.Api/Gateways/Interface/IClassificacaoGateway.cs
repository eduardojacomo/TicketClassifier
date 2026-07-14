using TicketClassifier.Api.Prompts;

namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>
/// Estratégia de classificação. Trabalha em LOTE — vários tickets por chamada —
/// para reduzir drasticamente o número de requisições ao provedor.
/// </summary>
public interface IClassificacaoGateway
{
    string Nome { get; }

    /// <summary>Classifica um lote; retorna os resultados na MESMA ordem da entrada.</summary>
    Task<IReadOnlyList<ClassificacaoResultado>> ClassificarLoteAsync(
        IReadOnlyList<TicketParaClassificar> itens, ClassificacaoPromptBuilder promptBuilder,
        CancellationToken ct = default,
        int loteAtual = 1, int totalLotes = 1, int totalTickets = 0);
}
