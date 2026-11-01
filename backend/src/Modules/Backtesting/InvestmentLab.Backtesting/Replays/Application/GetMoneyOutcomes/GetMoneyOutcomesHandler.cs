using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;

public sealed record GetMoneyOutcomesQuery(Guid StrategyId, int Version, decimal Start, decimal MonthlyContribution, int Years);

public sealed class GetMoneyOutcomesHandler(IBacktestRepository backtests)
{
    public async Task<Result<MoneyOutcomesDto>> Handle(GetMoneyOutcomesQuery query, CancellationToken cancellationToken)
    {
        var plan = MoneyPlan.TryOf(query.Start, query.MonthlyContribution, query.Years);
        if (plan.IsFailure)
            return plan.Error;

        var backtest = await backtests.FindAsync(query.StrategyId, query.Version, cancellationToken);
        if (backtest?.Result is not { } result)
            return BacktestErrors.NotReady(query.StrategyId);

        return new MoneyOutcomesDto(
            Contributed: plan.Value.Contributed,
            Years: plan.Value.Years,
            Strategy: MoneyOutcomes.Of(result.SeriesOf(ReplaySeriesKind.Strategy).Points, plan.Value),
            Benchmark: MoneyOutcomes.Of(result.SeriesOf(ReplaySeriesKind.Benchmark).Points, plan.Value));
    }
}
