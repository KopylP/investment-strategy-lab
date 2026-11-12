using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.MoveNodes;

public sealed class MoveNodesHandler(IDraftRepository drafts, TimeProvider clock) : IHandler
{
    public async Task<Result> Handle(Guid draftId, LayoutDto body, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        var layout = DraftMapping.ToLayout(body);
        if (layout.IsFailure)
            return layout.Error;

        draft.MoveNodes(layout.Value, clock.GetUtcNow());
        await drafts.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
