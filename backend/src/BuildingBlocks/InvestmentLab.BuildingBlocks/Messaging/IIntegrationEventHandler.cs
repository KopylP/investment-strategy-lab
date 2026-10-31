using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Messaging;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class
{
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);
}
