namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record FundScreenQuery(string? Exchange, string? Region, string? FundFamily, decimal? MinAssets, decimal? MaxExpenseRatio, IReadOnlyList<string> NameWords);
