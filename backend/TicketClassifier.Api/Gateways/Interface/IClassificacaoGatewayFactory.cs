namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>Fábrica que resolve a estratégia de classificação conforme a configuração.</summary>
public interface IClassificacaoGatewayFactory
{
    IClassificacaoGateway Criar();
}
