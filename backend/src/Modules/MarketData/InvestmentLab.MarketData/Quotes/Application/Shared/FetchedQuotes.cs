namespace InvestmentLab.MarketData.Quotes.Application.Shared;

public sealed record FetchedQuotes(IReadOnlyList<FetchedSeries> Series, IReadOnlyList<string> Missing);
