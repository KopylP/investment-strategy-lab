namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record TransferHint(Guid FromSubPortfolioId, Guid ToSubPortfolioId, decimal Amount);
