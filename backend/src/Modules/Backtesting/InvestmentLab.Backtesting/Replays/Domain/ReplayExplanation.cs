namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayExplanation(
    string Summary,
    string? EngineVersion = null,
    IReadOnlyList<ReplayCandidate>? Candidates = null,
    IReadOnlyDictionary<string, string>? ValueLabels = null);
