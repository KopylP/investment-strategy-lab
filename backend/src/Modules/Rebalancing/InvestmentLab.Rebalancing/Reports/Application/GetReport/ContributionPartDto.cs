using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ContributionPartDto(
    Guid SubPortfolioId,
    decimal Amount)
{
    public static ContributionPartDto From(ContributionPart contributionPart) => new(
        SubPortfolioId: contributionPart.SubPortfolioId,
        Amount: contributionPart.Amount);
}
