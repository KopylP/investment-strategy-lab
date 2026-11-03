using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class BacktestStatusQueries(IBacktestRepository backtests) : IBacktestStatusQueries
{
    public async Task<BacktestStatusDto?> GetStatusAsync(Guid strategyId, int version, CancellationToken cancellationToken)
    {
        var backtest = await backtests.FindAsync(strategyId, version, cancellationToken);
        if (backtest is null)
            return null;

        var summary = backtest.Result is { } result ? ToDto(result.StrategySummary) : null;

        return new BacktestStatusDto(backtest.Status.ToString(), backtest.RequestedAt, backtest.FinishedAt, backtest.FailureReason, summary);
    }

    private static BacktestSummaryDto ToDto(ReplaySummary summary) =>
        new(
            TotalReturn: summary.TotalReturn,
            AnnualReturn: summary.AnnualReturn,
            AnnualReturnAfterTax: summary.AnnualReturnAfterTax,
            Volatility: summary.Volatility,
            Sharpe: summary.Sharpe,
            MaxDrawdown: summary.MaxDrawdown);
}
