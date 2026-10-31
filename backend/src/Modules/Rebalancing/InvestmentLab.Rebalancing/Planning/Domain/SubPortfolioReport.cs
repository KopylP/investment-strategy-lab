namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioReport(Guid SubPortfolioId, string SubPortfolioName, string StrategyName, Cadence Default);
