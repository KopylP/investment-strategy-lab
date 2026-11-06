namespace InvestmentLab.Backtesting.Contracts;

public sealed record ExperimentSettingsDto(
    DateOnly Start,
    DateOnly End,
    decimal InitialCapital,
    decimal CommissionRate,
    decimal FeePerTrade,
    decimal SpreadBps,
    decimal SlippageBps,
    DateOnly? OutOfSampleStart = null,
    int? TrainingMonths = null,
    int? TestMonths = null,
    string? ParameterPath = null,
    IReadOnlyList<decimal>? ParameterValues = null);
