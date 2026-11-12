using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;

public sealed record RegisterInstrumentCommand(
    string Kind,
    string? Isin,
    string Name,
    string Exchange,
    string TradingCurrency,
    string? Group,
    bool? IsAccumulating,
    IReadOnlyList<string> ProviderSymbols,
    string? AssetClass = null,
    string? Region = null,
    string? AssetCurrency = null);

public sealed class RegisterInstrumentHandler(IInstrumentRepository instruments) : IHandler
{
    public async Task<Result<Guid>> Handle(RegisterInstrumentCommand command, CancellationToken cancellationToken)
    {
        var kind = EnumText.TryParse<InstrumentKind>(command.Kind, "instrument.kind_unknown");
        if (kind.IsFailure)
            return kind.Error;

        var isin = await FreeIsinAsync(command.Isin, cancellationToken);
        if (isin.IsFailure)
            return isin.Error;

        var currency = Currency.TryOf(command.TradingCurrency);
        if (currency.IsFailure)
            return currency.Error;

        var group = GroupOf(command.Group);
        if (group.IsFailure)
            return group.Error;

        var exposure = InstrumentExposure.TryOf(command.AssetClass, command.Region, command.AssetCurrency);
        if (exposure.IsFailure)
            return exposure.Error;

        var registered =
            Instrument.Register(kind.Value, isin.Value, command.Name, command.Exchange, currency.Value, group.Value, command.IsAccumulating);
        if (registered.IsFailure)
            return registered.Error;

        if (exposure.Value is { } known)
            registered.Value.AssignExposure(known);

        var assigned = await AssignSymbolsAsync(registered.Value, command.ProviderSymbols, cancellationToken);
        if (assigned.IsFailure)
            return assigned.Error;

        instruments.Add(registered.Value);
        await instruments.SaveChangesAsync(cancellationToken);

        return registered.Value.Id.Value;
    }

    private async Task<Result<Isin?>> FreeIsinAsync(string? text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<Isin?>.Success(null);

        var isin = Isin.TryOf(text);
        if (isin.IsFailure)
            return isin.Error;

        if (await instruments.IsinTakenAsync(isin.Value, cancellationToken))
            return InstrumentErrors.IsinTaken(isin.Value);

        return isin.Value;
    }

    private static Result<InstrumentGroup?> GroupOf(string? text)
    {
        if (text is null)
            return Result<InstrumentGroup?>.Success(null);

        var group = InstrumentGroup.TryOf(text);
        if (group.IsFailure)
            return group.Error;

        return group.Value;
    }

    private async Task<Result> AssignSymbolsAsync(Instrument instrument, IReadOnlyList<string> symbols, CancellationToken cancellationToken)
    {
        foreach (var text in symbols)
        {
            var symbol = ProviderSymbol.TryParse(text);
            if (symbol.IsFailure)
                return symbol.Error;

            if (await instruments.FindOwnerAsync(symbol.Value, cancellationToken) is not null)
                return InstrumentErrors.SymbolTaken(symbol.Value);

            instrument.AssignProviderSymbol(symbol.Value);
        }

        return Result.Success();
    }
}
