namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record SubPortfolioAdded(AccountId AccountId, SubPortfolioId SubPortfolioId, string Name, DateTimeOffset OccurredAt) : IDomainEvent;
