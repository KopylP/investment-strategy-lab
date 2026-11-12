namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed record TrialRunDto(
    int Number,
    Guid StrategyId,
    int Version,
    string? Label,
    DateTimeOffset RanAt,
    string Status,
    string? FailureReason,
    double? AnnualReturn,
    double? AnnualReturnAfterTax,
    double? Volatility,
    double? Sharpe,
    double? MaxDrawdown,
    Guid? ExperimentId = null,
    TrialExperimentDto? Experiment = null);
