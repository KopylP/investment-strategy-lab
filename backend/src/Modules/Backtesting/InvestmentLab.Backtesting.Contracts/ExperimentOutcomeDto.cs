namespace InvestmentLab.Backtesting.Contracts;

public sealed record ExperimentOutcomeDto(
    DateOnly Start,
    DateOnly End,
    BacktestSummaryDto Strategy,
    SeriesHorizonsDto StrategyHorizons,
    double? OutOfSampleSharpe,
    BacktestSummaryDto? Benchmark,
    double? BenchmarkAtEqualRiskSharpe);
