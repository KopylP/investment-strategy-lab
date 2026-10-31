using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Application.DescribeInstruments;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Application.Shared;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

internal sealed class InstrumentRegistry(
    IInstrumentRepository instruments, RegisterInstrumentHandler register, ISymbolLookup lookup, DescribeInstrumentsHandler describe)
        : IInstrumentRegistry
{
    public async Task<IReadOnlyList<ListedSymbolDto?>> ResolveAsync(IReadOnlyList<BrokerTickerDto> tickers, CancellationToken cancellationToken)
    {
        var matches = await lookup.LookupAsync([.. tickers.Select(ticker => ticker.IsYahooSymbol
            ? new SymbolQuery([ticker.Ticker.Trim().ToUpperInvariant()], null)
            : new SymbolQuery(Candidates(ticker.Ticker, ticker.Currency), ticker.Currency))], cancellationToken);
        return [.. matches.Select(match => match is null ? null : new ListedSymbolDto(match.Symbol, match.Name))];
    }

    public async Task<RegisteredInstrumentDto> RegisterAsync(NewInstrumentDto instrument, CancellationToken cancellationToken)
    {
        var symbol = ProviderSymbol.TryParse($"yfinance:{instrument.YahooSymbol.Trim().ToUpperInvariant()}");
        if (symbol.IsFailure)
            return new RegisteredInstrumentDto(null, symbol.Error.Message);

        if (await instruments.FindOwnerAsync(symbol.Value, cancellationToken) is { } owner)
            return new RegisteredInstrumentDto(owner.Value, null);

        var kind = Enum.TryParse<InstrumentKind>(instrument.Kind, ignoreCase: true, out var parsed) ? parsed : InstrumentKind.Stock;
        var exchange =
            instrument.YahooSymbol.Contains('.') ? instrument.YahooSymbol[(instrument.YahooSymbol.LastIndexOf('.') + 1)..].ToUpperInvariant() switch
            {
                "DE" => "XETRA",
                "L" => "LSE",
                "AS" => "Euronext Amsterdam",
                "PA" => "Euronext Paris",
                "MI" => "Borsa Italiana",
                "MC" => "BME",
                "BR" => "Euronext Brussels",
                "F" => "Frankfurt",
                "SW" => "SIX",
                "WA" => "GPW",
                var other => other,
            } : "US";

        var result = await register.Handle(new RegisterInstrumentCommand(
            kind, null, string.IsNullOrWhiteSpace(instrument.Name)
                ? instrument.YahooSymbol
                : instrument.Name.Trim(), exchange, instrument.TradingCurrency, null, null,
            [symbol.Value.ToString()]), cancellationToken);
        if (result.IsFailure)
            return new RegisteredInstrumentDto(null, result.Error.Message);

        await describe.Handle([InstrumentId.Of(result.Value)], cancellationToken);

        return new RegisteredInstrumentDto(result.Value, null);
    }

    private static IReadOnlyList<string> Candidates(string ticker, string currency)
    {
        var symbol = ticker.Trim().ToUpperInvariant();
        string[] suffixes = currency.ToUpperInvariant() switch
        {
            "EUR" => [".DE", ".AS", ".PA", ".MI", ".MC", ".BR", ".F"],
            "GBP" or "GBX" => [".L"],
            "CHF" => [".SW"],
            "PLN" => [".WA"],
            _ => [""],
        };

        return [.. suffixes.Select(suffix => suffix.Length == 0 ? symbol.Replace('.', '-') : symbol + suffix)];
    }
}
