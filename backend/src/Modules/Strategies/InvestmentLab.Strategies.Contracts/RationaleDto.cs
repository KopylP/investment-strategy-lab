namespace InvestmentLab.Strategies.Contracts;

public sealed record RationaleDto(
    string Summary,
    decimal? RiskFreeRate,
    IReadOnlyList<InstrumentScoreDto> Scores,
    string? Title = null,
    string? InstrumentLabel = null,
    IReadOnlyList<RationaleColumnDto>? Columns = null);
