namespace TicketClassifier.Api.Gateways.Interface;

/// <summary>Factory that resolves the classification strategy based on configuration.</summary>
public interface IClassificationGatewayFactory
{
    IClassificationGateway Create();
}
