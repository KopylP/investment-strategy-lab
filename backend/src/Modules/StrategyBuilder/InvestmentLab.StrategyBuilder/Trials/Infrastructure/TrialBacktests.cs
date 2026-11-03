using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal sealed class TrialBacktests(IBacktestStatusQueries backtests) : ITrialBacktests
{
    public async Task<TrialSummary> SummaryOfAsync(Guid strategyId, int version, CancellationToken cancellationToken)
    {
        var backtest = await backtests.GetStatusAsync(strategyId, version, cancellationToken);
        var status = StatusOf(backtest?.Status);
        var reason = backtest?.Status == "NotSupported" ? Message.Of("builder.trial.not_supported") : backtest?.FailureReason;
        var summary = backtest?.Summary;

        return new TrialSummary(
            Status: status,
            FailureReason: reason,
            AnnualReturn: summary?.AnnualReturn,
            AnnualReturnAfterTax: summary?.AnnualReturnAfterTax,
            Volatility: summary?.Volatility,
            Sharpe: summary?.Sharpe,
            MaxDrawdown: summary?.MaxDrawdown);
    }

    private static TrialStatus StatusOf(string? status) => status switch
    {
        null or "Queued" => TrialStatus.Waiting,
        "Running" => TrialStatus.Running,
        "Succeeded" => TrialStatus.Succeeded,
        "Failed" or "NotSupported" => TrialStatus.Failed,
        _ => throw new InvalidOperationException($"Unknown backtest status '{status}'."),
    };
}
