namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioPosition(Guid InstrumentId, Quantity Quantity, decimal? AverageCost);
