using InvestmentLab.SharedKernel;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.EnsureBacktest;

public sealed class EnsureBacktestHandler(IBacktestRepository backtests, IReplayedStrategies strategies, TimeProvider clock) : IHandler
{
    public async Task<Result<int>> Handle(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (await strategies.FindAsync(strategyId, version, cancellationToken) is not { } setup)
            return BacktestErrors.StrategyNotFound(strategyId);

        var now = clock.GetUtcNow();
        var existing = await backtests.FindAsync(setup.StrategyId, setup.Version, cancellationToken);

        if (existing is null)
            backtests.Add(Backtest.Request(setup.OwnerId, setup.StrategyId, setup.Version, now));
        else if (existing.IsDueAgain(now))
            existing.RequestAgain(now);

        await backtests.SaveChangesAsync(cancellationToken);

        return setup.Version;
    }
}
