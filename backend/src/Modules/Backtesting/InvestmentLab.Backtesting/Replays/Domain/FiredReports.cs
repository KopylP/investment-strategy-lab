namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record FiredReports(bool Rebalance, bool RiskReview, IReadOnlyList<ReplaySignalState>? States = null);
