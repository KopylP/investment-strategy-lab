using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Messaging;

internal sealed class IntegrationEventDelivery<TEvent> : IIntegrationEventDelivery
    where TEvent : class
{
    public string EventType => IntegrationEvents.TypeName<TEvent>();

    public async Task DeliverAsync(IServiceProvider services, string payload, CancellationToken cancellationToken)
    {
        var integrationEvent = JsonSerializer.Deserialize<TEvent>(payload, IntegrationEvents.Json)
            ?? throw new InvalidOperationException($"Empty payload for {EventType}.");

        foreach (var handler in services.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.Handle(integrationEvent, cancellationToken);
    }
}
