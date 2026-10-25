using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Application;

/// <summary>An external source of daily quotes (Yahoo Finance via the strategy engine, later others).</summary>
public interface IQuoteProvider
{
    QuoteProvider Provider { get; }

    /// <summary>Daily quotes from <paramref name="from"/> (inclusive) to the latest available.</summary>
    Task<FetchedQuotes> FetchAsync(IReadOnlyList<string> symbols, DateOnly from, CancellationToken cancellationToken);
}

public sealed record FetchedQuotes(IReadOnlyList<FetchedSeries> Series, IReadOnlyList<string> Missing);

public sealed record FetchedSeries(string Symbol, IReadOnlyList<FetchedQuote> Quotes);

/// <summary>A provider may miss either price on a day; such days are skipped.</summary>
public sealed record FetchedQuote(DateOnly Date, decimal? Close, decimal? AdjustedClose);
