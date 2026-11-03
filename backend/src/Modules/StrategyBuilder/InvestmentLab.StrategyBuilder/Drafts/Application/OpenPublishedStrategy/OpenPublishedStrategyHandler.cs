using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.OpenPublishedStrategy;

public sealed class OpenPublishedStrategyHandler(IDraftRepository drafts, IPublishedLayoutRepository layouts, IStrategyAuthoring authoring, IRuleVocabulary vocabularies, DraftDescriber describer, TimeProvider clock)
{
    public async Task<Result<DraftDto>> Handle(UserId owner, Guid strategyId, int version, CancellationToken cancellationToken)
    {
        var existing = await drafts.FindByOriginAsync(strategyId, cancellationToken);
        if (existing is not null)
            return await describer.DescribeAsync(existing, cancellationToken);

        var authored = await authoring.GetVersionAsync(strategyId, version, cancellationToken);
        if (authored is null)
            return DraftErrors.PublishedNotFound(strategyId, version);
        if (authored.TypeCode != RuleBasedType.Code)
            return DraftErrors.PublishedNotEditable();

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var saved = await layouts.FindAsync(strategyId, version, cancellationToken);
        var source = PublishedSource.Choose(saved, authored, vocabulary.Value);
        if (source.IsFailure)
            return source.Error;

        var origin = new PublishedOrigin(strategyId, version, authored.Name);
        var started = StrategyDraft.Start(owner, authored.Name, authored.Setup, source.Value.Graph, authored.Reports, origin, clock.GetUtcNow());
        if (started.IsFailure)
            return started.Error;

        var draft = started.Value;
        draft.MoveNodes(source.Value.Layout, clock.GetUtcNow());
        drafts.Add(draft);
        await drafts.SaveChangesAsync(cancellationToken);

        return await describer.DescribeAsync(draft, cancellationToken);
    }
}
