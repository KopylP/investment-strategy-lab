namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record ConditionKind(string Code, string Label, string Hint, string Phrase, IReadOnlyList<VocabularyField> Fields);
