using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.GetDraft;

public sealed class GetDraftHandler(IDraftRepository drafts, DraftDescriber describer)
{
    public async Task<Result<DraftDto>> Handle(Guid draftId, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        return await describer.DescribeAsync(draft, cancellationToken);
    }
}
