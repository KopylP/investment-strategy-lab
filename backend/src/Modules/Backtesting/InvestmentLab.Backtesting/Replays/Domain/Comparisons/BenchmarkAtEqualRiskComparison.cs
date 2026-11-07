namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public sealed class BenchmarkAtEqualRiskComparison : IReplayComparison
{
    private static readonly Guid MixedHolding = new("b8a3c1d2-4f6e-4a7b-9c0d-2e1f3a5b7c9d");

    public const string SeriesCode = "benchmark-at-equal-risk";

    public string Code => SeriesCode;

    public ComparedLine LineOf(ReplayedHistory history)
    {
        var benchmark = history.Setup.Benchmark;
        var benchmarkGrowth = history.Prices.GrowthOf(benchmark.InstrumentId, history.FirstDay);

        var share = BenchmarkShareAtRiskOf(history.StrategyGrowth, benchmarkGrowth);
        var growth = MixedGrowth(history, share);

        var label = Message.Of("backtest.series.benchmark-at-equal-risk", new { name = benchmark.Label, share });

        return new ComparedLine(Code, label, growth, HeldAs: MixedHolding);
    }

    private static double BenchmarkShareAtRiskOf(IReadOnlyList<GrowthDay> strategy, IReadOnlyList<GrowthDay> benchmark)
    {
        var benchmarkVolatility = ReplaySeries.VolatilityOf(benchmark);
        if (benchmarkVolatility <= 0d)
            return 1d;

        var strategyVolatility = ReplaySeries.VolatilityOf(strategy);

        return Math.Min(1d, strategyVolatility / benchmarkVolatility);
    }

    private static List<GrowthDay> MixedGrowth(ReplayedHistory history, double benchmarkShare)
    {
        var benchmark = history.Setup.Benchmark.InstrumentId;
        var cash = history.Setup.CashInstrumentId;
        var growth = new List<GrowthDay>();
        var value = 1d;

        for (var dayIndex = history.FirstDay; dayIndex < history.Prices.Days.Count; dayIndex++)
        {
            if (dayIndex > history.FirstDay)
            {
                var benchmarkPart = benchmarkShare * history.ReturnOn(benchmark, dayIndex);
                var cashPart = (1d - benchmarkShare) * history.ReturnOn(cash, dayIndex);
                value *= 1d + benchmarkPart + cashPart;
            }

            growth.Add(new GrowthDay(history.Prices.Days[dayIndex], value));
        }

        return growth;
    }
}
