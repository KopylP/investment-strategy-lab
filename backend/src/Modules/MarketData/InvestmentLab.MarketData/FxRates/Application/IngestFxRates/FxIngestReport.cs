namespace InvestmentLab.MarketData.FxRates.Application.IngestFxRates;

public sealed record FxIngestReport(IReadOnlyDictionary<string, int> RatesAdded);
