namespace InvestmentLab.MarketData.Quotes.Application;

public sealed record FetchedQuotes(IReadOnlyList<FetchedSeries> Series, IReadOnlyList<string> Missing);
