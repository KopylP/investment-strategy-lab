namespace InvestmentLab.MarketData.Quotes.Domain;

public sealed record DailyQuotesIngested(IReadOnlyList<Guid> InstrumentIds, DateOnly FirstTradingDate, DateTimeOffset OccurredAt) : IDomainEvent;
