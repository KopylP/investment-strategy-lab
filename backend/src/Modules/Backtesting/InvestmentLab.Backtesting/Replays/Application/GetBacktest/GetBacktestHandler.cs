using InvestmentLab.Backtesting.Replays.Application.Shared;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetBacktest;

public sealed class GetBacktestHandler(IBacktestRepository backtests)
{
    public async Task<Result<BacktestDto>> Handle(Guid strategyId, int version, CancellationToken cancellationToken)
    {
        if (await backtests.FindAsync(strategyId, version, cancellationToken) is not { } backtest)
            return BacktestErrors.StrategyNotFound(strategyId);

        return new BacktestDto(
            StrategyId: backtest.StrategyId,
            StrategyVersion: backtest.StrategyVersion,
            Status: backtest.Status.ToString(),
            RequestedAt: backtest.RequestedAt,
            FinishedAt: backtest.FinishedAt,
            FailureReason: backtest.FailureReason,
            Result: backtest.Result is { } result ? ReplayResultDto.From(result) : null);
    }
}
