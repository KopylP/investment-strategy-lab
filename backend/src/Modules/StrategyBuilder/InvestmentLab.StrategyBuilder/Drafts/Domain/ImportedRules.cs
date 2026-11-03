namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record ImportedRules(StrategyGraph Graph, IReadOnlyList<Guid> ExtraSeries);
