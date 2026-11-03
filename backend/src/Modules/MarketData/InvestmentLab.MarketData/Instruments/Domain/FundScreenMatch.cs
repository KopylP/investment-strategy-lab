namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record FundScreenMatch(string Symbol, string Name, string? Currency, string? Exchange, string? FundFamily, decimal? TotalAssets, decimal? ExpenseRatio);
