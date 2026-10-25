using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

/// <summary>Registers an instrument under its Yahoo symbol; an instrument that already owns the symbol is returned as it is.</summary>
internal sealed class InstrumentRegistry(IInstrumentRepository instruments, RegisterInstrumentHandler register) : IInstrumentRegistry
{
    public async Task<RegisteredInstrumentDto> RegisterAsync(NewInstrumentDto instrument, CancellationToken cancellationToken)
    {
        var symbol = ProviderSymbol.TryParse($"yfinance:{instrument.YahooSymbol.Trim().ToUpperInvariant()}");
        if (symbol.IsFailure)
            return new RegisteredInstrumentDto(null, symbol.Error.Message);
        if (await instruments.FindOwnerAsync(symbol.Value, cancellationToken) is { } owner)
            return new RegisteredInstrumentDto(owner.Value, null);

        var kind = Enum.TryParse<InstrumentKind>(instrument.Kind, ignoreCase: true, out var parsed) ? parsed : InstrumentKind.Stock;
        var exchange = instrument.YahooSymbol.Contains('.') ? instrument.YahooSymbol[(instrument.YahooSymbol.LastIndexOf('.') + 1)..].ToUpperInvariant() switch
        {
            "DE" => "XETRA",
            "L" => "LSE",
            "AS" => "Euronext Amsterdam",
            "PA" => "Euronext Paris",
            "MI" => "Borsa Italiana",
            var other => other,
        } : "US";

        var result = await register.Handle(new RegisterInstrumentCommand(
            kind, null, string.IsNullOrWhiteSpace(instrument.Name) ? instrument.YahooSymbol : instrument.Name.Trim(), exchange, instrument.TradingCurrency, null, null,
            [symbol.Value.ToString()]), cancellationToken);
        return result.IsSuccess ? new RegisteredInstrumentDto(result.Value, null) : new RegisteredInstrumentDto(null, result.Error.Message);
    }
}
