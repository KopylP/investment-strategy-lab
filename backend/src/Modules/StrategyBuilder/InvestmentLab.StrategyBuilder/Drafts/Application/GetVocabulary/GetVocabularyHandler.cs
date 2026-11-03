using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.GetVocabulary;

public sealed record NodeKindDto(string Kind, IReadOnlyList<InputSocket> Inputs, string? Output);

public sealed record TemplateDto(string Code, string Name, string Description);

public sealed record VocabularyDto(
    IReadOnlyList<IndicatorKind> Indicators,
    IReadOnlyList<ConditionKind> Conditions,
    IReadOnlyDictionary<string, VocabularyField> BlockFields,
    IReadOnlyList<NodeKindDto> NodeKinds,
    IReadOnlyList<TemplateDto> Templates);

public sealed class GetVocabularyHandler(IRuleVocabulary vocabularies)
{
    public async Task<Result<VocabularyDto>> Handle(CancellationToken cancellationToken)
    {
        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var nodeKinds = Enum.GetValues<NodeKind>().Select(NodeKindOf).ToList();
        var templates = DraftTemplates.Codes.Select(TemplateOf).ToList();

        return new VocabularyDto(vocabulary.Value.Indicators, vocabulary.Value.Conditions, vocabulary.Value.BlockFields, nodeKinds, templates);
    }

    private static NodeKindDto NodeKindOf(NodeKind kind) => new(kind.ToString(), NodeSockets.InputsOf(kind), NodeSockets.OutputOf(kind)?.ToString());

    private static TemplateDto TemplateOf(string code) =>
        new(code, Message.Of($"builder.template.{code}.name"), Message.Of($"builder.template.{code}.description"));
}
