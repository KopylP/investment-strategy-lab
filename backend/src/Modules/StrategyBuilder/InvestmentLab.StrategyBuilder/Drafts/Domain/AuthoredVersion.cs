namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record AuthoredVersion(string Name, string Rules, DraftSetup Setup, IReadOnlyList<DraftReport> Reports, string TypeCode, int Version);
