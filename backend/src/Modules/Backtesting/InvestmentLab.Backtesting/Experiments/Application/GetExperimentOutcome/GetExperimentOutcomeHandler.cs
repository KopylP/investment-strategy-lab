using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Domain.Comparisons;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentOutcome;

public sealed class GetExperimentOutcomeHandler(IExperimentRepository experiments) : IHandler
{
    public async Task<ExperimentOutcomeDto?> Handle(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(id, cancellationToken);
        if (experiment?.Result is not { } report)
            return null;

        var baseline = report.Baseline;
        var strategy = baseline.SeriesOf(ReplaySeries.StrategyCode);
        var horizons = SeriesHorizons.Of(baseline.Start, strategy.Points);
        var benchmark = SeriesOrNull(baseline, BenchmarkComparison.SeriesCode);
        var benchmarkAtEqualRisk = SeriesOrNull(baseline, BenchmarkAtEqualRiskComparison.SeriesCode);

        return new ExperimentOutcomeDto(
            Start: baseline.Start,
            End: baseline.End,
            Strategy: SummaryOf(strategy.Summary),
            StrategyHorizons: HorizonsOf(horizons),
            OutOfSampleSharpe: OutOfSampleSharpeOf(report),
            Benchmark: benchmark is null ? null : SummaryOf(benchmark.Summary),
            BenchmarkAtEqualRiskSharpe: benchmarkAtEqualRisk?.Summary.Sharpe);
    }

    private static ReplaySeries? SeriesOrNull(ReplayResult result, string code) =>
        result.Series.FirstOrDefault(series => series.Code == code);

    private static double? OutOfSampleSharpeOf(ResearchReport report) =>
        report.Studies.FirstOrDefault(study => study.Kind == ResearchStudyKinds.OutOfSample)?.Summary?.Sharpe;

    private static BacktestSummaryDto SummaryOf(ReplaySummary summary) => new(
        TotalReturn: summary.TotalReturn,
        AnnualReturn: summary.AnnualReturn,
        AnnualReturnAfterTax: summary.AnnualReturnAfterTax,
        Volatility: summary.Volatility,
        Sharpe: summary.Sharpe,
        MaxDrawdown: summary.MaxDrawdown);

    private static SeriesHorizonsDto HorizonsOf(SeriesHorizons horizons)
    {
        var years = horizons.Years.Select(year => new CalendarYearReturnDto(year.Year, year.Return, year.Partial)).ToList();

        return new SeriesHorizonsDto(horizons.OneYear, horizons.ThreeYearsAnnual, horizons.FiveYearsAnnual, years);
    }
}
