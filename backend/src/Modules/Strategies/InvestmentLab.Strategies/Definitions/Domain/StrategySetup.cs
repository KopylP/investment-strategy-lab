namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record StrategySetup(
    string Parameters,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    MarketSeriesRefs Market,
    IReadOnlyList<VersionReport>? Reports = null);
