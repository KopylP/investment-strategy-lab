using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.DiscardDraft;

public sealed class DiscardDraftHandler(IDraftRepository drafts)
{
    public async Task<Result> Handle(Guid draftId, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return Result.Success();

        drafts.Remove(draft);
        await drafts.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
