namespace InvestmentLab.MarketData.Quotes.Application;

public sealed record FetchedQuote(DateOnly Date, decimal? Close, decimal? AdjustedClose);
