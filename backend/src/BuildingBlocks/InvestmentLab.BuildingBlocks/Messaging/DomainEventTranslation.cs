using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Messaging;

public interface IDomainEventTranslation
{
    Type DomainEventType { get; }

    void PublishTo(ModuleDbContext context, IDomainEvent domainEvent);
}
