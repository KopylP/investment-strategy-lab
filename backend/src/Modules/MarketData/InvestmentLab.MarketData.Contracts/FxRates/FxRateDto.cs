namespace InvestmentLab.MarketData.Contracts.FxRates;

public sealed record FxRateDto(string Source, string Base, string Quote, DateOnly EffectiveDate, decimal Rate, string? TableNumber);
