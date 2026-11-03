namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record TrialRun(int Number, Guid StrategyId, int StrategyVersion, string Fingerprint, string? Label, DateTimeOffset RanAt);
