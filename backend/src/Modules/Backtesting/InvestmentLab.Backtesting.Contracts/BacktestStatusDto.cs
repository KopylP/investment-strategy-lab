namespace InvestmentLab.Backtesting.Contracts;

public sealed record BacktestStatusDto(string Status, DateTimeOffset RequestedAt, DateTimeOffset? FinishedAt, string? FailureReason, BacktestSummaryDto? Summary);
