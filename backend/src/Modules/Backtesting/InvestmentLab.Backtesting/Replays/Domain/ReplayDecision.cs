namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayDecision(bool MarketIsFavorable, decimal InvestFraction, IReadOnlyList<ReplayTarget> Targets, ReplayExplanation? Explanation = null);
