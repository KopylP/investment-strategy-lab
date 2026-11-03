using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Application.DescribeInstruments;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Application.RegisterListedFund;
using InvestmentLab.MarketData.Instruments.Application.Shared;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

internal sealed class InstrumentRegistry(
    IInstrumentRepository instruments,
    RegisterInstrumentHandler register,
    RegisterListedFundHandler registerFund,
    ISymbolLookup lookup,
    DescribeInstrumentsHandler describe,
    ISymbolConvention convention)
        : IInstrumentRegistry
{
    public async Task<IReadOnlyList<BrokerTickerMatchDto>> MatchAsync(IReadOnlyList<BrokerTickerDto> tickers, CancellationToken cancellationToken)
    {
        var known = await instruments.ListAsync(cancellationToken);
        var matcher = new BrokerTickerMatcher(known, convention);

        return [.. tickers.Select(ticker => MatchOf(ticker, matcher))];
    }

    private BrokerTickerMatchDto MatchOf(BrokerTickerDto ticker, BrokerTickerMatcher matcher)
    {
        var instrument = matcher.Find(ticker.Ticker, ticker.Currency);
        var suggested = convention.CandidatesFor(ticker.Ticker, ticker.Currency)[0];

        return new BrokerTickerMatchDto(instrument?.ToDto(), suggested);
    }

    public async Task<IReadOnlyList<ListedSymbolDto?>> ResolveAsync(IReadOnlyList<BrokerTickerDto> tickers, CancellationToken cancellationToken)
    {
        var queries = tickers.Select(QueryOf).ToList();
        var matches = await lookup.LookupAsync(queries, cancellationToken);

        return [.. matches.Select(match => match is null ? null : new ListedSymbolDto(match.Symbol, match.Name))];
    }

    private SymbolQuery QueryOf(BrokerTickerDto ticker) => ticker.IsExactSymbol
        ? new SymbolQuery([ticker.Ticker.Trim().ToUpperInvariant()], null)
        : new SymbolQuery(convention.CandidatesFor(ticker.Ticker, ticker.Currency), ticker.Currency);

    public async Task<RegisteredInstrumentDto> RegisterAsync(NewInstrumentDto instrument, CancellationToken cancellationToken)
    {
        var symbol = ProviderSymbol.TryParse($"{QuoteProvider.Primary}:{instrument.Symbol.Trim().ToUpperInvariant()}");
        if (symbol.IsFailure)
            return new RegisteredInstrumentDto(null, symbol.Error.AsMessage());

        if (await instruments.FindOwnerAsync(symbol.Value, cancellationToken) is { } owner)
            return new RegisteredInstrumentDto(owner.Value, null);

        var command = RegisterCommandOf(instrument, symbol.Value);

        var result = command.Kind == InstrumentKind.Etf
            ? await registerFund.Handle(new RegisterListedFundCommand(instrument.Symbol, instrument.Name, instrument.TradingCurrency), cancellationToken)
            : await register.Handle(command, cancellationToken);
        if (result.IsFailure)
            return new RegisteredInstrumentDto(null, result.Error.AsMessage());

        await describe.Handle([InstrumentId.Of(result.Value)], cancellationToken);

        return new RegisteredInstrumentDto(result.Value, null);
    }

    private RegisterInstrumentCommand RegisterCommandOf(NewInstrumentDto instrument, ProviderSymbol symbol)
    {
        var kind = Enum.TryParse<InstrumentKind>(instrument.Kind, ignoreCase: true, out var parsed) ? parsed : InstrumentKind.Stock;
        var name = string.IsNullOrWhiteSpace(instrument.Name) ? instrument.Symbol : instrument.Name.Trim();

        return new RegisterInstrumentCommand(
            kind,
            null,
            name,
            convention.ExchangeOf(instrument.Symbol),
            instrument.TradingCurrency,
            null,
            null,
            [symbol.ToString()]);
    }
}
