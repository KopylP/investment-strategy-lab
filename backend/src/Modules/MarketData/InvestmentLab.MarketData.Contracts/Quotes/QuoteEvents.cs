namespace InvestmentLab.MarketData.Contracts.Quotes;

/// <summary>New daily quotes were stored for these instruments.</summary>
public sealed record DailyQuotesIngestedV1(DateTimeOffset IngestedAt, IReadOnlyList<Guid> InstrumentIds, DateOnly FirstTradingDate);

/// <summary>A quote was fixed by hand; anything computed from it on or after the date is outdated.</summary>
public sealed record QuoteCorrectedV1(Guid InstrumentId, DateOnly TradingDate);
