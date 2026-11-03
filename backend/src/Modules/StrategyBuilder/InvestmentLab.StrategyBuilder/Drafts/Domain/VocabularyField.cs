using System.Text.Json;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record VocabularyField(
    string Name,
    FieldKind Kind,
    string Label,
    double? Minimum,
    double? Maximum,
    bool Required,
    JsonElement? Default,
    IReadOnlyDictionary<string, string> ChoiceLabels);
