namespace InvestmentLab.MarketData.Instruments.Domain;

public static class InstrumentErrors
{
    public static Error IsinRequired(InstrumentKind kind) =>
        Error.Validation("instrument.isin_required", $"An instrument of kind {kind} needs an ISIN.");

    public static Error NameRequired() => Error.Validation("instrument.name_required", "Instrument name must not be blank.");

    public static Error ExchangeRequired() => Error.Validation("instrument.exchange_required", "Exchange must not be blank.");

    public static Error InvalidProviderSymbol(string? text) =>
        Error.Validation("instrument.provider_symbol_invalid", $"'{text}' is not a provider symbol like 'yfinance:SXR8.DE'.");

    public static Error NotFound(Guid id) => Error.NotFound("instrument.not_found", $"Instrument {id} does not exist.");

    public static Error IsinTaken(Isin isin) => Error.Conflict("instrument.isin_taken", $"An instrument with ISIN {isin} is already registered.");

    public static Error SymbolTaken(ProviderSymbol symbol) =>
        Error.Conflict("instrument.symbol_taken", $"Symbol {symbol} already belongs to another instrument.");
}
