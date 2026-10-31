using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.Integration;

internal sealed class StrategyReconfiguredMapper : IDomainEventMapper<StrategyReconfigured, StrategyReconfiguredV1>
{
    public StrategyReconfiguredV1 Map(StrategyReconfigured reconfigured) =>
        new(reconfigured.StrategyId.Value, reconfigured.OwnerId.Value, reconfigured.Version, reconfigured.OccurredAt);
}
