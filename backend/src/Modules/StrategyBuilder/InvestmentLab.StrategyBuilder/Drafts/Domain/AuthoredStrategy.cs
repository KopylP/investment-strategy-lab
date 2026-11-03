namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record AuthoredStrategy(string Rules, DraftSetup Setup, IReadOnlyList<DraftReport> Reports);
