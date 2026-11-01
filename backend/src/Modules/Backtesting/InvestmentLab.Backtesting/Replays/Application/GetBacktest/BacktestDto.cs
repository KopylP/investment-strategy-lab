using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.GetBacktest;

public sealed record BacktestDto(
    Guid StrategyId,
    int StrategyVersion,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? FinishedAt,
    string? FailureReason,
    ReplayResult? Result);
