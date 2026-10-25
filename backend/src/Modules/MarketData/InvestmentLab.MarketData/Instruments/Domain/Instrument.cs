namespace InvestmentLab.MarketData.Instruments.Domain;

/// <summary>A tradable fund or a reference series (index, FX pair) the platform keeps quotes for.</summary>
public sealed class Instrument : AggregateRoot<InstrumentId>
{
    private readonly List<ProviderSymbol> _providerSymbols = [];

    private Instrument(InstrumentId id) : base(id) { }

    public InstrumentKind Kind { get; private set; }

    /// <summary>Required for ETFs: taxes, broker statements and issuers know funds by ISIN.</summary>
    public Isin? Isin { get; private set; }

    public string Name { get; private set; } = "";

    /// <summary>Market identifier code of the listing (XETR, ARCX, XNAS…).</summary>
    public string Exchange { get; private set; } = "";

    public Currency TradingCurrency { get; private set; } = Currency.EUR;

    public InstrumentGroup? Group { get; private set; }

    /// <summary>Accumulating fund (reinvests dividends); null when unknown or not a fund.</summary>
    public bool? IsAccumulating { get; private set; }

    public IReadOnlyList<ProviderSymbol> ProviderSymbols => _providerSymbols;

    public static Result<Instrument> Register(
        InstrumentKind kind, Isin? isin, string name, string exchange, Currency tradingCurrency, InstrumentGroup? group, bool? isAccumulating)
    {
        if (kind == InstrumentKind.Etf && isin is null)
            return InstrumentErrors.IsinRequired(kind);
        if (string.IsNullOrWhiteSpace(name))
            return InstrumentErrors.NameRequired();
        if (string.IsNullOrWhiteSpace(exchange))
            return InstrumentErrors.ExchangeRequired();

        return new Instrument(InstrumentId.New())
        {
            Kind = kind,
            Isin = isin,
            Name = name.Trim(),
            Exchange = exchange.Trim().ToUpperInvariant(),
            TradingCurrency = tradingCurrency,
            Group = group,
            IsAccumulating = isAccumulating,
        };
    }

    /// <summary>One symbol per provider: a new symbol for the same provider replaces the old one.</summary>
    public void AssignProviderSymbol(ProviderSymbol symbol)
    {
        _providerSymbols.RemoveAll(existing => existing.Provider == symbol.Provider);
        _providerSymbols.Add(symbol);
    }

    public ProviderSymbol? SymbolFor(QuoteProvider provider) =>
        _providerSymbols.FirstOrDefault(symbol => symbol.Provider == provider);
}
