using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.Integration;

internal sealed class StrategyReconfiguredMapper : IDomainEventMapper<StrategyReconfigured, StrategyReconfiguredV1>
{
    public StrategyReconfiguredV1 Map(StrategyReconfigured reconfigured) => new(
        StrategyId: reconfigured.StrategyId.Value,
        OwnerId: reconfigured.OwnerId.Value,
        Version: reconfigured.Version,
        ReconfiguredAt: reconfigured.OccurredAt,
        IsTrial: reconfigured.IsTrial);
}
