namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public sealed class BenchmarkComparison : IReplayComparison
{
    public const string SeriesCode = "benchmark";

    public string Code => SeriesCode;

    public ComparedLine LineOf(ReplayedHistory history)
    {
        var benchmark = history.Setup.Benchmark;

        var growth = history.Prices.GrowthOf(benchmark.InstrumentId, history.FirstDay);
        var label = Message.Of("backtest.series.benchmark", new { name = benchmark.Label });

        return new ComparedLine(Code, label, growth, HeldAs: benchmark.InstrumentId);
    }
}
