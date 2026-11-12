using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.CopyEntryToExit;

public sealed record CopyEntryToExitCommand(int Revision, double Margin);

public sealed class CopyEntryToExitHandler(IDraftRepository drafts, IRuleVocabulary vocabularies, DraftDescriber describer, TimeProvider clock) : IHandler
{
    public async Task<Result<DraftDto>> Handle(Guid draftId, int revision, double margin, CancellationToken cancellationToken)
    {
        if (!ExitFromEntry.AcceptsMargin(margin))
            return DraftErrors.CopyMarginOutOfRange();

        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var graph = ExitFromEntry.Of(draft.Graph, vocabulary.Value, margin);
        var change = new DraftChange(revision, null, null, null, graph, null);
        var changed = draft.Change(change, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await drafts.SaveChangesAsync(cancellationToken);

        return await describer.DescribeAsync(draft, cancellationToken);
    }
}
