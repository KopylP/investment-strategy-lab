using System.Text.Json;

namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SectionScore(
    Guid InstrumentId,
    string Name,
    string Group,
    decimal? Price,
    decimal? Momentum,
    decimal? Return1m,
    decimal? Return3m,
    decimal? Return12m,
    decimal? Volatility,
    bool? AboveMovingAverage,
    int? Rank,
    string Status,
    string Reason,
    IReadOnlyDictionary<string, JsonElement>? Values = null);
