using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Messaging;

public sealed class DomainEventTranslation<TDomainEvent, TIntegrationEvent>(IDomainEventMapper<TDomainEvent, TIntegrationEvent> mapper)
    : IDomainEventTranslation
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : class
{
    public Type DomainEventType => typeof(TDomainEvent);

    public void PublishTo(ModuleDbContext context, IDomainEvent domainEvent)
    {
        var integrationEvent = mapper.Map((TDomainEvent)domainEvent);

        context.Publish(integrationEvent, domainEvent.OccurredAt);
    }
}
