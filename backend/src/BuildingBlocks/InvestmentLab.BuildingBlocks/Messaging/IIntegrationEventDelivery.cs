using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Messaging;

internal interface IIntegrationEventDelivery
{
    string EventType { get; }

    Task DeliverAsync(IServiceProvider services, string payload, CancellationToken cancellationToken);
}
