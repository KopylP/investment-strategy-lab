namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record AmountAllocationDto(Guid SubPortfolioId, decimal Amount);
