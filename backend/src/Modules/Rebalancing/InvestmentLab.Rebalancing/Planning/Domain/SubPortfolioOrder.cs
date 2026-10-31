namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioOrder(Guid SubPortfolioId, PlannedOrder Order);
