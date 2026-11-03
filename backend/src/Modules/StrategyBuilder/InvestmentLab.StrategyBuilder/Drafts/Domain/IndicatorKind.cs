namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record IndicatorKind(string Code, string Label, string Hint, string Phrase, string ColumnFormat, IReadOnlyList<VocabularyField> Fields);
