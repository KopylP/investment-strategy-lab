namespace InvestmentLab.Backtesting.Contracts;

public sealed record ExperimentStatusDto(string Status, string? FailureReason, BacktestSummaryDto? Summary);
