namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record SubPortfolioReport(Guid SubPortfolioId, string SubPortfolioName, string StrategyName, Cadence Default);
