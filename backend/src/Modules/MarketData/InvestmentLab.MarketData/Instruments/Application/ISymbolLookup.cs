namespace InvestmentLab.MarketData.Instruments.Application;

/// <summary>Asks the quote source (Yahoo Finance via the strategy engine) which symbols exist.</summary>
public interface ISymbolLookup
{
    /// <summary>Per query, in order: its first candidate that exists in the query's currency, or null.</summary>
    Task<IReadOnlyList<SymbolMatch?>> LookupAsync(IReadOnlyList<SymbolQuery> queries, CancellationToken cancellationToken);
}

/// <param name="Candidates">Yahoo symbols to try in order, e.g. <c>ASME.DE</c>, <c>ASME.AS</c>.</param>
public sealed record SymbolQuery(IReadOnlyList<string> Candidates, string? Currency);

/// <param name="QuoteType">Yahoo's EQUITY, ETF, ...</param>
public sealed record SymbolMatch(string Symbol, string Name, string Currency, string QuoteType);
