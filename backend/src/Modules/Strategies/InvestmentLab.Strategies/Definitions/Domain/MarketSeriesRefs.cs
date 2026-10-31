namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record MarketSeriesRefs(Guid BenchmarkInstrumentId, Guid? VolatilityIndexInstrumentId);
