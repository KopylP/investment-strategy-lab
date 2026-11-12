using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Application.CleanUpTrials;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.PublishDraft;

public sealed record PublishedVersionDto(Guid StrategyId, int Version);
public sealed record PublishDraftCommand(int Revision);

public sealed class PublishDraftHandler(
    IDraftRepository drafts,
    IPublishedLayoutRepository layouts,
    ITrialCleanupRepository cleanups,
    IRuleVocabulary vocabularies,
    IOwnSignals signals,
    IStrategyAuthoring authoring,
    CleanUpTrialHandler cleanTrial,
    TimeProvider clock,
    ILogger<PublishDraftHandler> logger) : IHandler
{
    public async Task<Result<PublishedVersionDto>> Handle(Guid draftId, int revision, CancellationToken cancellationToken)
    {
        var draft = await drafts.FindAsync(draftId, cancellationToken);
        if (draft is null)
            return DraftErrors.NotFound(draftId);
        if (draft.Revision != revision)
            return DraftErrors.Stale();

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var problems = GraphCheck.Of(draft, vocabulary.Value, await signals.AllAsync(cancellationToken));
        if (DraftCheck.HasErrors(problems))
            return Refused(problems);

        var rules = RulesGenerator.Generate(draft);
        var strategy = new AuthoredStrategy(rules.Json, SetupBindings.EvaluationSetup(draft), draft.Reports);
        var published = await authoring.PublishAsync(draft.OwnerId, draft.Origin, draft.Name, strategy, cancellationToken);
        if (published.IsFailure)
            return PublicationFailure(published.Error, rules, draft.Graph);

        var layout = PublishedLayout.Of(draft.OwnerId, published.Value, draft.Graph, draft.Layout);
        layouts.Add(layout);
        drafts.Remove(draft);
        var cleanup = QueueCleanup(draft.TrialStrategyId);
        await drafts.SaveChangesAsync(cancellationToken);

        if (cleanup is not null)
            await TryCleanupAsync(cleanup.Id, cancellationToken);

        return new PublishedVersionDto(published.Value.StrategyId, published.Value.Version);
    }

    private TrialCleanup? QueueCleanup(Guid? strategyId)
    {
        if (strategyId is null)
            return null;

        var cleanup = TrialCleanup.For(strategyId.Value, clock.GetUtcNow());
        cleanups.Add(cleanup);

        return cleanup;
    }

    private async Task TryCleanupAsync(Guid cleanupId, CancellationToken cancellationToken)
    {
        try
        {
            await cleanTrial.Handle(cleanupId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Published draft cleanup {CleanupId} remains queued", cleanupId);
        }
    }

    private static Error PublicationFailure(Error error, GeneratedRules rules, StrategyGraph graph)
    {
        if (error.Parameters is not RulesRefused refused)
            return error;

        var placed = EngineProblems.Placed(refused.Problems, rules.SourceMap, graph);

        return Refused(placed);
    }

    private static Error Refused(IReadOnlyList<DraftProblem> problems)
    {
        var placed = problems.Select(DraftMapping.ToDto).ToList();

        return DraftErrors.HasProblems(new { problems = placed });
    }
}
