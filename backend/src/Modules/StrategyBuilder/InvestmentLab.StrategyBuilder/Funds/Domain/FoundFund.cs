namespace InvestmentLab.StrategyBuilder.Funds.Domain;

public sealed record FoundFund(string Symbol, string Name, string? Currency, string? FundFamily, decimal? TotalAssets, decimal? ExpenseRatio, Guid? InstrumentId);
