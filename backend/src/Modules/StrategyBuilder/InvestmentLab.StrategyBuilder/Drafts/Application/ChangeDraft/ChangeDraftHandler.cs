using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.ChangeDraft;

public sealed class ChangeDraftHandler(IDraftRepository drafts, DraftDescriber describer, TimeProvider clock)
{
    public async Task<Result<DraftDto>> Handle(Guid draftId, DraftChangeDto body, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        var change = DraftMapping.ToChange(body);
        if (change.IsFailure)
            return change.Error;

        var changed = draft.Change(change.Value, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await drafts.SaveChangesAsync(cancellationToken);

        return await describer.DescribeAsync(draft, cancellationToken);
    }
}
