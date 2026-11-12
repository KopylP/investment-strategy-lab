using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record TransferHintDto(
    Guid FromSubPortfolioId,
    Guid ToSubPortfolioId,
    decimal Amount)
{
    public static TransferHintDto From(TransferHint transferHint) => new(
        FromSubPortfolioId: transferHint.FromSubPortfolioId,
        ToSubPortfolioId: transferHint.ToSubPortfolioId,
        Amount: transferHint.Amount);
}
