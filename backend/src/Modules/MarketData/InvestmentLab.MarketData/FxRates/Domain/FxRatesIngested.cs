namespace InvestmentLab.MarketData.FxRates.Domain;

public sealed record FxRatesIngested(DateOnly FirstEffectiveDate, DateTimeOffset OccurredAt) : IDomainEvent;
