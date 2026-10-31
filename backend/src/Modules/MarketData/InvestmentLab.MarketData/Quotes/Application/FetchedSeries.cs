namespace InvestmentLab.MarketData.Quotes.Application;

public sealed record FetchedSeries(string Symbol, IReadOnlyList<FetchedQuote> Quotes);
