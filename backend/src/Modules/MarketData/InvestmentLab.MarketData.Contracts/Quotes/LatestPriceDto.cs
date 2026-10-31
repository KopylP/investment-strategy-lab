namespace InvestmentLab.MarketData.Contracts.Quotes;

public sealed record LatestPriceDto(Guid InstrumentId, DateOnly Date, decimal Close, decimal AdjustedClose, string Currency, bool IsStale);
