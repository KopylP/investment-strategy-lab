using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.DiscardDraft;

public sealed class DiscardDraftHandler(IDraftRepository drafts, ITrialCleanupRepository cleanups, TimeProvider clock)
{
    public async Task<Result> Handle(Guid draftId, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return Result.Success();

        if (draft.TrialStrategyId is { } trialStrategyId)
            cleanups.Add(TrialCleanup.For(trialStrategyId, clock.GetUtcNow()));

        drafts.Remove(draft);
        await drafts.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
