namespace InvestmentLab.MarketData.Contracts.Quotes;

public sealed record QuoteSeriesDto(Guid InstrumentId, IReadOnlyList<QuotePointDto> Points);
