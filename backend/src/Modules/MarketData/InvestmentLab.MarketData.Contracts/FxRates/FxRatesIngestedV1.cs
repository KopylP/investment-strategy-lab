namespace InvestmentLab.MarketData.Contracts.FxRates;

public sealed record FxRatesIngestedV1(DateTimeOffset IngestedAt, DateOnly FirstEffectiveDate);
