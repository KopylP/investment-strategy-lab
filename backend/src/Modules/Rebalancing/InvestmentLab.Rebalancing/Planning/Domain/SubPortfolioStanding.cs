namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioStanding(Guid SubPortfolioId, Percentage TargetShare, decimal Value);
