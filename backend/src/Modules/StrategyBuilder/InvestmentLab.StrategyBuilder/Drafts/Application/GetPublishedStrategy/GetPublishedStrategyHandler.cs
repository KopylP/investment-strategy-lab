using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.GetPublishedStrategy;

public sealed record PublishedStrategyDto(bool Editable, Guid? DraftId, IReadOnlyList<BlockWordsDto> Words);

public sealed class GetPublishedStrategyHandler(IDraftRepository drafts, IPublishedLayoutRepository layouts, IStrategyAuthoring authoring, IRuleVocabulary vocabularies, DraftDescriber describer, TimeProvider clock)
{
    public async Task<Result<PublishedStrategyDto>> Handle(UserId owner, Guid strategyId, int version, CancellationToken cancellationToken)
    {
        var authored = await authoring.GetVersionAsync(strategyId, version, cancellationToken);
        if (authored is null)
            return DraftErrors.PublishedNotFound(strategyId, version);
        if (authored.TypeCode != RuleBasedType.Code)
            return new PublishedStrategyDto(false, null, []);

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var saved = await layouts.FindAsync(strategyId, version, cancellationToken);
        var source = PublishedSource.Choose(saved, authored, vocabulary.Value);
        if (source.IsFailure)
            return source.Error;

        var started = StrategyDraft.Start(owner, authored.Name, authored.Setup, source.Value.Graph, authored.Reports, null, clock.GetUtcNow());
        if (started.IsFailure)
            return started.Error;

        var described = await describer.DescribeAsync(started.Value, cancellationToken);
        if (described.IsFailure)
            return described.Error;

        var existing = await drafts.FindByOriginAsync(strategyId, cancellationToken);

        return new PublishedStrategyDto(true, existing?.Id, described.Value.Words);
    }
}
