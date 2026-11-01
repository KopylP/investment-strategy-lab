using System.Globalization;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class PriceConversion
{
    private const int RateDigits = 4;
    private const decimal SameCurrency = 1m;

    public static IReadOnlyList<string> ForeignCurrencies(IEnumerable<IQuotedPrice> quotes, string currency) =>
        [.. quotes.Where(quote => !IsIn(quote, currency)).Select(quote => quote.Currency).Distinct()];

    public static PricesInCurrency Of(IReadOnlyList<IQuotedPrice> quotes, IReadOnlyDictionary<string, decimal?> rates, string currency)
    {
        var warnings = ForeignCurrencies(quotes, currency).Select(foreign => WarningOf(foreign, currency, rates[foreign])).ToList();
        var prices = new Dictionary<Guid, IInstrumentPrice>();

        foreach (var quote in quotes)
        {
            if (FactorOf(quote, rates, currency) is { } factor)
                prices[quote.InstrumentId] = new ConvertedPrice(quote, factor);
        }

        return new PricesInCurrency(prices, warnings);
    }

    private static bool IsIn(IQuotedPrice quote, string currency) =>
        string.Equals(quote.Currency, currency, StringComparison.OrdinalIgnoreCase);

    private static decimal? FactorOf(IQuotedPrice quote, IReadOnlyDictionary<string, decimal?> rates, string currency) =>
        IsIn(quote, currency) ? SameCurrency : rates[quote.Currency];

    private static string WarningOf(string currency, string accountCurrency, decimal? rate) => rate is { } known
        ? Message.Of("report.warning.fx_converted", new { currency, accountCurrency, rate = decimal.Round(known, RateDigits).ToString(CultureInfo.InvariantCulture) })
        : Message.Of("report.warning.fx_missing", new { currency, accountCurrency });

    private sealed record ConvertedPrice(IQuotedPrice Quote, decimal Factor) : IInstrumentPrice
    {
        public decimal Close => Quote.Close * Factor;
        public DateOnly Date => Quote.Date;
        public bool IsStale => Quote.IsStale;
    }
}
