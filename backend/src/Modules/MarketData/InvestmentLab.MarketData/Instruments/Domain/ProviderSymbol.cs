namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed record ProviderSymbol
{
    public QuoteProvider Provider { get; }
    public string Symbol { get; }

    private ProviderSymbol(QuoteProvider provider, string symbol) => (Provider, Symbol) = (provider, symbol);

    public static ProviderSymbol Of(QuoteProvider provider, string symbol) =>
        string.IsNullOrWhiteSpace(symbol) ? throw new ArgumentException("Blank symbol.", nameof(symbol)) : new(provider, symbol.Trim());

    public static Result<ProviderSymbol> TryParse(string? text)
    {
        var parts = text?.Trim().Split(':', 2);
        if (parts is not [var provider, var symbol] || string.IsNullOrWhiteSpace(symbol))
            return InstrumentErrors.InvalidProviderSymbol(text);

        var parsedProvider = QuoteProvider.TryOf(provider);

        return parsedProvider.IsSuccess ? Of(parsedProvider.Value, symbol) : InstrumentErrors.InvalidProviderSymbol(text);
    }

    public override string ToString() => $"{Provider}:{Symbol}";
}
