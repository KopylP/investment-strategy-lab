using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Messaging;

public interface IDomainEventMapper<in TDomainEvent, out TIntegrationEvent>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : class
{
    TIntegrationEvent Map(TDomainEvent domainEvent);
}
