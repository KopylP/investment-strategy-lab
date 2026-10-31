using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.AssignProviderSymbol;

public sealed record AssignProviderSymbolCommand(Guid InstrumentId, string ProviderSymbol);

public sealed class AssignProviderSymbolHandler(IInstrumentRepository instruments)
{
    public async Task<Result> Handle(AssignProviderSymbolCommand command, CancellationToken cancellationToken)
    {
        var symbol = ProviderSymbol.TryParse(command.ProviderSymbol);
        if (symbol.IsFailure)
            return symbol.Error;

        if (command.InstrumentId == Guid.Empty
            || await instruments.FindAsync(InstrumentId.Of(command.InstrumentId), cancellationToken) is not { } instrument)
            return InstrumentErrors.NotFound(command.InstrumentId);

        if (await instruments.FindOwnerAsync(symbol.Value, cancellationToken) is { } owner && owner != instrument.Id)
            return InstrumentErrors.SymbolTaken(symbol.Value);

        instrument.AssignProviderSymbol(symbol.Value);
        await instruments.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
