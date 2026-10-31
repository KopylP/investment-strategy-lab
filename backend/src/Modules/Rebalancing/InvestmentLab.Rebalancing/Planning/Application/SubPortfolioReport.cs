namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed record SubPortfolioReport(Guid SubPortfolioId, string SubPortfolioName, string StrategyName, Cadence Default);
