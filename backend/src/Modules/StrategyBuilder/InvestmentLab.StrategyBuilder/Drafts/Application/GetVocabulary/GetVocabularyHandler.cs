using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.GetVocabulary;

public sealed record VocabularyFieldDto(
    string Name,
    string Kind,
    string Label,
    double? Minimum,
    double? Maximum,
    bool Required,
    JsonElement? Default,
    IReadOnlyDictionary<string, string> ChoiceLabels);

public sealed record IndicatorKindDto(string Code, string Label, string Hint, string Phrase, string ColumnFormat, IReadOnlyList<VocabularyFieldDto> Fields);

public sealed record ConditionKindDto(string Code, string Label, string Hint, string Phrase, IReadOnlyList<VocabularyFieldDto> Fields);

public sealed record InputSocketDto(string Name, string Type, bool Required, bool Many);

public sealed record NodeKindDto(string Kind, IReadOnlyList<InputSocketDto> Inputs, string? Output);

public sealed record ReportKindDto(string Code, string Name, string Description, IReadOnlyList<string> SignalKinds);

public sealed record TemplateDto(string Code, string Name, string Description);

public sealed record VocabularyDto(
    IReadOnlyList<IndicatorKindDto> Indicators,
    IReadOnlyList<ConditionKindDto> Conditions,
    IReadOnlyDictionary<string, VocabularyFieldDto> BlockFields,
    IReadOnlyList<NodeKindDto> NodeKinds,
    IReadOnlyList<ReportKindDto> Reports,
    IReadOnlyList<TemplateDto> Templates);

public sealed class GetVocabularyHandler(IRuleVocabulary vocabularies)
{
    public async Task<Result<VocabularyDto>> Handle(CancellationToken cancellationToken)
    {
        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var indicators = vocabulary.Value.Indicators.Select(ToDto).ToList();
        var conditions = vocabulary.Value.Conditions.Select(ToDto).ToList();
        var blockFields = vocabulary.Value.BlockFields.ToDictionary(entry => entry.Key, entry => ToDto(entry.Value));
        var nodeKinds = Enum.GetValues<NodeKind>().Select(NodeKindOf).ToList();
        var reports = vocabulary.Value.Reports.Select(ToDto).ToList();
        var templates = DraftTemplates.Codes.Select(TemplateOf).ToList();

        return new VocabularyDto(indicators, conditions, blockFields, nodeKinds, reports, templates);
    }

    private static IndicatorKindDto ToDto(IndicatorKind kind) =>
        new(kind.Code, kind.Label, kind.Hint, kind.Phrase, kind.ColumnFormat, [.. kind.Fields.Select(ToDto)]);

    private static ConditionKindDto ToDto(ConditionKind kind) =>
        new(kind.Code, kind.Label, kind.Hint, kind.Phrase, [.. kind.Fields.Select(ToDto)]);

    private static VocabularyFieldDto ToDto(VocabularyField field) =>
        new(field.Name, field.Kind.ToString(), field.Label, field.Minimum, field.Maximum, field.Required, field.Default, field.ChoiceLabels);

    private static NodeKindDto NodeKindOf(NodeKind kind)
    {
        var inputs = NodeSockets.InputsOf(kind).Select(input => new InputSocketDto(input.Name, input.Type.ToString(), input.Required, input.Many));

        return new NodeKindDto(kind.ToString(), [.. inputs], NodeSockets.OutputOf(kind)?.ToString());
    }

    private static ReportKindDto ToDto(DeclaredDraftReport report) =>
        new(report.Code, report.Name, report.Description, report.SignalKinds);

    private static TemplateDto TemplateOf(string code) =>
        new(code, Message.Of($"builder.template.{code}.name"), Message.Of($"builder.template.{code}.description"));
}
