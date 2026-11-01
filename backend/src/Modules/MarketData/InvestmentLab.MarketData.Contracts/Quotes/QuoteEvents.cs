namespace InvestmentLab.MarketData.Contracts.Quotes;

public sealed record QuoteHistoryExtendedV1(DateTimeOffset ExtendedAt);

public sealed record DailyQuotesIngestedV1(DateTimeOffset IngestedAt, IReadOnlyList<Guid> InstrumentIds, DateOnly FirstTradingDate);
