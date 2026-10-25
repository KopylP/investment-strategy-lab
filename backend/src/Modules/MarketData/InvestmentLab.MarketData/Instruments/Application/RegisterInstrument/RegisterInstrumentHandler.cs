using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;

public sealed record RegisterInstrumentCommand(
    InstrumentKind Kind,
    string? Isin,
    string Name,
    string Exchange,
    string TradingCurrency,
    string? Group,
    bool? IsAccumulating,
    IReadOnlyList<string> ProviderSymbols);

public sealed class RegisterInstrumentHandler(IInstrumentRepository instruments)
{
    public async Task<Result<Guid>> Handle(RegisterInstrumentCommand command, CancellationToken cancellationToken)
    {
        Isin? isin = null;
        if (!string.IsNullOrWhiteSpace(command.Isin))
        {
            var parsedIsin = Isin.TryOf(command.Isin);
            if (parsedIsin.IsFailure)
                return parsedIsin.Error;
            isin = parsedIsin.Value;

            if (await instruments.IsinTakenAsync(isin, cancellationToken))
                return InstrumentErrors.IsinTaken(isin);
        }

        var currency = Currency.TryOf(command.TradingCurrency);
        if (currency.IsFailure)
            return currency.Error;

        InstrumentGroup? group = null;
        if (command.Group is not null)
        {
            var parsedGroup = InstrumentGroup.TryOf(command.Group);
            if (parsedGroup.IsFailure)
                return parsedGroup.Error;
            group = parsedGroup.Value;
        }

        var registered = Instrument.Register(command.Kind, isin, command.Name, command.Exchange, currency.Value, group, command.IsAccumulating);
        if (registered.IsFailure)
            return registered.Error;

        foreach (var text in command.ProviderSymbols)
        {
            var symbol = ProviderSymbol.TryParse(text);
            if (symbol.IsFailure)
                return symbol.Error;
            if (await instruments.FindOwnerAsync(symbol.Value, cancellationToken) is not null)
                return InstrumentErrors.SymbolTaken(symbol.Value);

            registered.Value.AssignProviderSymbol(symbol.Value);
        }

        instruments.Add(registered.Value);
        await instruments.SaveChangesAsync(cancellationToken);
        return registered.Value.Id.Value;
    }
}
