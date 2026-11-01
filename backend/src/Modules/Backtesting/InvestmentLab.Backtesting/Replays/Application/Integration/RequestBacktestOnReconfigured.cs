using InvestmentLab.Backtesting.Replays.Application.EnsureBacktest;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Backtesting.Replays.Application.Integration;

internal sealed class RequestBacktestOnReconfigured(EnsureBacktestHandler ensure) : IIntegrationEventHandler<StrategyReconfiguredV1>
{
    public async Task Handle(StrategyReconfiguredV1 reconfigured, CancellationToken cancellationToken) =>
        await ensure.Handle(reconfigured.StrategyId, reconfigured.Version, cancellationToken);
}
