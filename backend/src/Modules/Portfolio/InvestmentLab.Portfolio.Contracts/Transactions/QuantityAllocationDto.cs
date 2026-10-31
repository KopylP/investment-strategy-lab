namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record QuantityAllocationDto(Guid SubPortfolioId, decimal Quantity);
