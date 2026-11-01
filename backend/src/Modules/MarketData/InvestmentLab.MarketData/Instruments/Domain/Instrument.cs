namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed class Instrument : AggregateRoot<InstrumentId>
{
    private readonly List<ProviderSymbol> _providerSymbols = [];

    private Instrument(InstrumentId id) : base(id) { }

    public InstrumentKind Kind { get; private set; }

    public Isin? Isin { get; private set; }

    public string Name { get; private set; } = "";

    public string Exchange { get; private set; } = "";

    public Currency TradingCurrency { get; private set; } = Currency.EUR;

    public InstrumentGroup? Group { get; private set; }

    public bool? IsAccumulating { get; private set; }

    public IReadOnlyList<ProviderSymbol> ProviderSymbols => _providerSymbols;

    public string? Description { get; private set; }

    public DateTimeOffset? DescribedAt { get; private set; }

    public static readonly TimeSpan DescriptionLifetime = TimeSpan.FromDays(30);

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

    public void AssignProviderSymbol(ProviderSymbol symbol)
    {
        _providerSymbols.RemoveAll(existing => existing.Provider == symbol.Provider);
        _providerSymbols.Add(symbol);
    }

    public bool NeedsDescription(DateTimeOffset now) => DescribedAt is null || now - DescribedAt.Value >= DescriptionLifetime;

    public void Describe(string? description, DateTimeOffset at)
    {
        if (!string.IsNullOrWhiteSpace(description))
            Description = description.Trim();

        DescribedAt = at;
    }

    public string? Ticker => SymbolFor(QuoteProvider.Primary)?.Symbol;

    public ProviderSymbol? SymbolFor(QuoteProvider provider) =>
        _providerSymbols.FirstOrDefault(symbol => symbol.Provider == provider);
}
