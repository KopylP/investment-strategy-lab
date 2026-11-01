namespace InvestmentLab.MarketData.Instruments.Domain;

public sealed class BrokerTickerMatcher(IReadOnlyList<Instrument> instruments, ISymbolConvention convention)
{
    public Instrument? Find(string ticker, string currency)
    {
        var exact = instruments.Where(instrument => IsExact(instrument, ticker)).ToList();
        var listed = instruments.Where(instrument => IsListed(instrument, ticker)).ToList();

        return InCurrency(exact, currency)
            ?? InCurrency(listed, currency)
            ?? instruments.FirstOrDefault(instrument => exact.Contains(instrument) || listed.Contains(instrument));
    }

    private static bool IsExact(Instrument instrument, string ticker) =>
        string.Equals(instrument.Ticker, ticker, StringComparison.OrdinalIgnoreCase);

    private bool IsListed(Instrument instrument, string ticker) =>
        instrument.Ticker is { } symbol && convention.IsListingOf(symbol, ticker);

    private static Instrument? InCurrency(List<Instrument> candidates, string currency) =>
        candidates.FirstOrDefault(instrument => instrument.TradingCurrency.Code == currency);
}
