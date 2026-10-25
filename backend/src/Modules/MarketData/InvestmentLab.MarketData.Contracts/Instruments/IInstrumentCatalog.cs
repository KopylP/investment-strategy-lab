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
