namespace InvestmentLab.Strategies.Contracts;

public sealed record MarketRegimeDto(
    bool IsFavorable,
    decimal? BenchmarkPrice,
    decimal? BenchmarkMovingAverage,
    decimal? VolatilityIndex,
    IReadOnlyList<string> Reasons);
