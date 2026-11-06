using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.ListDrafts;

public sealed record DraftSummaryDto(Guid Id, string Name, string? OriginStrategyName, int? OriginVersion, DateTimeOffset UpdatedAt, DateTimeOffset? LastTrialAt);

public sealed class ListDraftsHandler(IDraftRepository drafts)
{
    public async Task<IReadOnlyList<DraftSummaryDto>> Handle(CancellationToken cancellationToken)
    {
        var all = await drafts.ListAsync(cancellationToken);

        return [.. all.Select(SummaryOf)];
    }

    private static DraftSummaryDto SummaryOf(StrategyDraft draft) =>
        new(draft.Id, draft.Name, draft.Origin?.StrategyName, draft.Origin?.Version, draft.UpdatedAt, (draft.Trials.Count > 0 ? draft.Trials[^1].RanAt : null));
}
