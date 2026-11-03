using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.Integration;

internal sealed class StrategyDeletedMapper : IDomainEventMapper<StrategyDeleted, StrategyDeletedV1>
{
    public StrategyDeletedV1 Map(StrategyDeleted deleted) =>
        new(deleted.StrategyId.Value, deleted.OwnerId.Value, deleted.OccurredAt);
}
