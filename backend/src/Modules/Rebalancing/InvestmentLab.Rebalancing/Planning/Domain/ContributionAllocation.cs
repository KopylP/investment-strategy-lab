namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record ContributionAllocation(
    decimal Total, ContributionSplit Split, IReadOnlyList<ContributionShare> Shares, TransferSuggestion? Transfer)
{
    public decimal For(Guid subPortfolioId) => Shares.FirstOrDefault(share => share.SubPortfolioId == subPortfolioId)?.Amount ?? 0m;
}
