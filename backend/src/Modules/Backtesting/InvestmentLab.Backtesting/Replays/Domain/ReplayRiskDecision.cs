namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayRiskDecision(IReadOnlyList<Guid> Exits, ReplayExplanation? Explanation = null);
