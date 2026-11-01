namespace InvestmentLab.MarketData.Quotes.Domain;

public sealed record QuoteHistoryExtended(DateTimeOffset OccurredAt) : IDomainEvent;
