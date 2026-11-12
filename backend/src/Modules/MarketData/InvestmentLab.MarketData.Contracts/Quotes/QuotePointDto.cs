namespace InvestmentLab.MarketData.Contracts.Quotes;

public sealed record QuotePointDto(
    DateOnly Date,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    decimal AdjustedClose,
    IReadOnlyList<string> Flags);
