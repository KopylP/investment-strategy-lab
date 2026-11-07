using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal sealed class TrialBacktests(IBacktestStatusQueries backtests, IBacktestRequests requests) : ITrialBacktests
{
    public async Task<TrialSummary> SummaryOfAsync(Guid strategyId, int version, CancellationToken cancellationToken)
    {
        var backtest = await backtests.GetStatusAsync(strategyId, version, cancellationToken);
        var status = BacktestStatuses.TrialStatusOf(backtest?.Status);
        var reason = backtest?.Status == BacktestStatuses.NotSupported ? Message.Of("builder.trial.not_supported") : backtest?.FailureReason;
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

    public Task RequestAsync(TrialVersion version, CancellationToken cancellationToken) =>
        requests.EnsureAsync(version.StrategyId, version.Version, cancellationToken);
}
