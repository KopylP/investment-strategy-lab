using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.RegisterListedFund;

public sealed record RegisterListedFundCommand(string Symbol, string Name, string TradingCurrency);

public sealed class RegisterListedFundHandler(IInstrumentRepository instruments, ISymbolConvention convention)
{
    public async Task<Result<Guid>> Handle(RegisterListedFundCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Symbol))
            return InstrumentErrors.InvalidProviderSymbol(command.Symbol);

        var symbol = ProviderSymbol.Of(QuoteProvider.Primary, command.Symbol.Trim().ToUpperInvariant());
        var known = await instruments.FindOwnerAsync(symbol, cancellationToken);
        if (known is not null)
            return known.Value.Value;

        var currency = Currency.TryOf(command.TradingCurrency);
        if (currency.IsFailure)
            return currency.Error;

        var exchange = convention.ExchangeOf(symbol.Symbol);
        var registered = Instrument.FromFundListing(command.Name, exchange, currency.Value, symbol);
        if (registered.IsFailure)
            return registered.Error;

        instruments.Add(registered.Value);
        await instruments.SaveChangesAsync(cancellationToken);

        return registered.Value.Id.Value;
    }
}
