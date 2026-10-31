using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Domain;

public sealed record QuoteCorrected(InstrumentId InstrumentId, DateOnly TradingDate, DateTimeOffset OccurredAt) : IDomainEvent;
