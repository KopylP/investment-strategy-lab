using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Integration;

internal sealed class DeleteBacktestsOfDeletedStrategy(IBacktestRepository backtests, IExperimentRepository experiments) : IIntegrationEventHandler<StrategyDeletedV1>
{
    public async Task Handle(StrategyDeletedV1 deleted, CancellationToken cancellationToken)
    {
        var ofStrategy = await backtests.OfStrategyAsync(deleted.StrategyId, cancellationToken);

        foreach (var backtest in ofStrategy)
            backtests.Remove(backtest);

        await backtests.SaveChangesAsync(cancellationToken);
        await experiments.RemoveOfStrategyAsync(deleted.StrategyId, cancellationToken);
    }
}
