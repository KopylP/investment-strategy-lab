namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed class YFinanceSymbolConvention : ISymbolConvention
{
    public const string VolatilityIndexSymbol = "^VIX";

    private const char ListingSeparator = '.';
    private const char ClassSeparator = '-';
    private const string UnitedStates = "US";

    private static readonly Dictionary<string, string> ExchangeBySuffix = new()
    {
        ["DE"] = "XETRA",
        ["L"] = "LSE",
        ["AS"] = "Euronext Amsterdam",
        ["PA"] = "Euronext Paris",
        ["MI"] = "Borsa Italiana",
        ["MC"] = "BME",
        ["BR"] = "Euronext Brussels",
        ["F"] = "Frankfurt",
        ["SW"] = "SIX",
        ["WA"] = "GPW",
    };

    public IReadOnlyList<string> CandidatesFor(string ticker, string currency)
    {
        var symbol = ticker.Trim().ToUpperInvariant();

        string[] suffixes = currency.ToUpperInvariant() switch
        {
            "EUR" => [".DE", ".AS", ".PA", ".MI", ".MC", ".BR", ".F"],
            "GBP" or "GBX" => [".L"],
            "CHF" => [".SW"],
            "PLN" => [".WA"],
            _ => [],
        };

        return suffixes.Length == 0
            ? [symbol.Replace(ListingSeparator, ClassSeparator)]
            : [.. suffixes.Select(suffix => symbol + suffix)];
    }

    public string ExchangeOf(string symbol)
    {
        var separator = symbol.LastIndexOf(ListingSeparator);
        if (separator < 0)
            return UnitedStates;

        var suffix = symbol[(separator + 1)..].ToUpperInvariant();

        return ExchangeBySuffix.GetValueOrDefault(suffix, suffix);
    }

    public bool IsListingOf(string symbol, string ticker) =>
        symbol.StartsWith(ticker + ListingSeparator, StringComparison.OrdinalIgnoreCase);
}
