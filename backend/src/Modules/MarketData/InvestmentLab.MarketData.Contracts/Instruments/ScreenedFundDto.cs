namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record ScreenedFundDto(string Symbol, string Name, string? Currency, string? Exchange, string? FundFamily, decimal? TotalAssets, decimal? ExpenseRatio, Guid? RegisteredInstrumentId);
