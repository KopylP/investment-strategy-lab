namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record RuleVocabulary(
    IReadOnlyList<IndicatorKind> Indicators,
    IReadOnlyList<ConditionKind> Conditions,
    IReadOnlyDictionary<string, VocabularyField> BlockFields,
    IReadOnlyList<DeclaredDraftReport> Reports)
{
    public IndicatorKind? Indicator(string code) => Indicators.FirstOrDefault(indicator => indicator.Code == code);

    public ConditionKind? Condition(string code) => Conditions.FirstOrDefault(condition => condition.Code == code);
}
