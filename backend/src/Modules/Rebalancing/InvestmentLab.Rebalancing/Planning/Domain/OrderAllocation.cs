namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record OrderAllocation(Guid SubPortfolioId, OrderKind Kind, decimal Quantity, string Reason);
