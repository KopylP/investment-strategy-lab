namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record RulesCheck(IReadOnlyList<EngineProblem> Problems, IReadOnlyList<Guid> SignalIds);
