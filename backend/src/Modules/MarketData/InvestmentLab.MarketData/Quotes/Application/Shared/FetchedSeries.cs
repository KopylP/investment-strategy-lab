namespace InvestmentLab.MarketData.Quotes.Application.Shared;

public sealed record FetchedSeries(string Symbol, IReadOnlyList<FetchedQuote> Quotes);
