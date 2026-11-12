namespace InvestmentLab.MarketData.Quotes.Application.Shared;

public sealed record FetchedQuote(
    DateOnly Date,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal? Close,
    decimal? AdjustedClose);
