using System.Text.Json;

namespace InvestmentLab.Strategies.Contracts;

public sealed record InstrumentScoreDto(
    Guid InstrumentId,
    string Name,
    string Group,
    decimal? Price,
    decimal? Momentum,
    decimal? Return1m,
    decimal? Return3m,
    decimal? Return12m,
    decimal? Volatility,
    decimal? Sharpe,
    decimal? Drawdown,
    bool? AboveMovingAverage,
    DateOnly? LastDate,
    int? Rank,
    string Status,
    string Reason,
    decimal? Weight,
    IReadOnlyDictionary<string, JsonElement>? Values = null);
