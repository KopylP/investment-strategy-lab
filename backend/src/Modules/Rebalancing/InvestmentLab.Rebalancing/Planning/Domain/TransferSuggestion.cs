namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record TransferSuggestion(Guid FromSubPortfolioId, Guid ToSubPortfolioId, decimal Amount);
