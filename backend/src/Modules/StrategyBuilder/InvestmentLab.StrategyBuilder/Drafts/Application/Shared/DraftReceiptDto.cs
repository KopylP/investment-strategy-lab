namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftReceiptDto(Guid Id, string Name, int Revision, IReadOnlyList<DraftProblemDto> Problems)
{
    public static DraftReceiptDto Of(DraftDto draft) => new(draft.Id, draft.Name, draft.Revision, draft.Problems);
}
