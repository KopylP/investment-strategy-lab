namespace InvestmentLab.MarketData.Contracts.Instruments;

public interface IInstrumentRegistry
{
    Task<RegisteredInstrumentDto> RegisterAsync(NewInstrumentDto instrument, CancellationToken cancellationToken);

    Task<IReadOnlyList<ListedSymbolDto?>> ResolveAsync(IReadOnlyList<BrokerTickerDto> tickers, CancellationToken cancellationToken);
}
