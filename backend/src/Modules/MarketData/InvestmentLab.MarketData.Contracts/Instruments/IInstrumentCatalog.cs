namespace InvestmentLab.MarketData.Contracts.Instruments;

/// <summary>Instruments the platform knows, for other modules (universes, holdings, reports).</summary>
public interface IInstrumentCatalog
{
    Task<IReadOnlyList<InstrumentDto>> GetInstruments(CancellationToken cancellationToken);
}

/// <param name="Kind">Etf, Index or FxPair.</param>
/// <param name="ProviderSymbols">Provider-qualified symbols, e.g. <c>yfinance:SXR8.DE</c>.</param>
public sealed record InstrumentDto(
    Guid Id,
    string Kind,
    string? Isin,
    string Name,
    string Exchange,
    string TradingCurrency,
    string? Group,
    bool? IsAccumulating,
    IReadOnlyList<string> ProviderSymbols);

/// <summary>Adds an instrument the catalog does not know yet (e.g. a stock bought outside the strategies).</summary>
public interface IInstrumentRegistry
{
    Task<RegisteredInstrumentDto> RegisterAsync(NewInstrumentDto instrument, CancellationToken cancellationToken);
}

/// <param name="YahooSymbol">E.g. <c>AAPL</c> or <c>QDVE.DE</c>; quotes are loaded nightly under it.</param>
/// <param name="Kind">Etf or Stock.</param>
public sealed record NewInstrumentDto(string YahooSymbol, string Name, string TradingCurrency, string Kind);

/// <summary>Either the new (or already registered) instrument's id, or why it could not be registered.</summary>
public sealed record RegisteredInstrumentDto(Guid? InstrumentId, string? Error);
