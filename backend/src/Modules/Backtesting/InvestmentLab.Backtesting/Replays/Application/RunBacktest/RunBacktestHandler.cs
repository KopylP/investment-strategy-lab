using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.RunBacktest;

public sealed class RunBacktestHandler(IBacktestRepository backtests, IReplayedStrategies strategies, StrategyReplay replay, TimeProvider clock)
{
    public async Task Handle(Guid backtestId, CancellationToken cancellationToken)
    {
        if (await backtests.FindAsync(backtestId, cancellationToken) is not { } backtest)
            return;

        var setup = await strategies.FindAsync(backtest.StrategyId, backtest.StrategyVersion, cancellationToken);

        if (setup is null)
            backtest.Fail(Message.Of(BacktestErrors.StrategyNotFound(backtest.StrategyId).Code), clock.GetUtcNow());
        else if (!setup.CanBeReplayed)
            backtest.MarkNotSupported(clock.GetUtcNow());
        else
            await ReplayAsync(backtest, setup, cancellationToken);

        await backtests.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplayAsync(Backtest backtest, ReplaySetup setup, CancellationToken cancellationToken)
    {
        backtest.Start();
        await backtests.SaveChangesAsync(cancellationToken);

        var replayed = await replay.RunAsync(setup, cancellationToken);

        if (replayed.IsSuccess)
            backtest.Complete(replayed.Value, clock.GetUtcNow());
        else
            backtest.Fail(Message.Of(replayed.Error.Code), clock.GetUtcNow());
    }
}
