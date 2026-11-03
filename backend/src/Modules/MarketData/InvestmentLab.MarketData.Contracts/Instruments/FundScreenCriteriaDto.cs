namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record FundScreenCriteriaDto(string? ExchangeId, string? RegionId, string? FundFamily, decimal? MinAssets, decimal? MaxExpenseRatio, IReadOnlyList<string> NameWords);
