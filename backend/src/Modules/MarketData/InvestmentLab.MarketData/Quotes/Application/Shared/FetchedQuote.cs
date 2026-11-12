namespace InvestmentLab.MarketData.Quotes.Application.Shared;

public sealed record FetchedQuote(DateOnly Date, decimal? Close, decimal? AdjustedClose);
