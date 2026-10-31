namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ActionAllocation(Guid SubPortfolioId, decimal Quantity, string? Reason);
