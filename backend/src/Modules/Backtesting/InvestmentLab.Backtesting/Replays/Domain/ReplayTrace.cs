namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayTrace(IReadOnlyList<GrowthDay> StrategyGrowth, IReadOnlyList<GrowthDay> CashGrowth)
{
    private const int LeastDaysForSummary = 2;

    public ReplaySummary? SummaryBetween(DateOnly start, DateOnly end)
    {
        var strategy = Between(StrategyGrowth, start, end);
        if (strategy.Count < LeastDaysForSummary)
            return null;

        var cash = Between(CashGrowth, start, end);
        var riskFreeRate = cash.Count > 1 ? ReplaySeries.AnnualReturnOf(cash) : 0d;

        return ReplaySeries.Of(ReplaySeries.StrategyCode, ReplaySeries.StrategyCode, strategy, riskFreeRate).Summary;
    }

    private static List<GrowthDay> Between(IReadOnlyList<GrowthDay> growth, DateOnly start, DateOnly end) =>
        [.. growth.Where(day => day.Date >= start && day.Date <= end)];
}
