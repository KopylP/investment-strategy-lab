namespace InvestmentLab.Portfolio.Contracts.Accounts;

public sealed record SubPortfolioAddedV1(Guid AccountId, Guid SubPortfolioId, string Name);
