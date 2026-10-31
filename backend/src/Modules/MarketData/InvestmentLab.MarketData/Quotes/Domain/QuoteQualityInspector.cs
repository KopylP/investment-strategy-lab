namespace InvestmentLab.MarketData.Quotes.Domain;

public static class QuoteQualityInspector
{
    public const decimal SpikeThreshold = 0.20m;
    public const int MaxMissingBusinessDays = 5;
    public const int MaxStaleBusinessDays = 5;

    public static void Inspect(IReadOnlyList<DailyQuote> quotes, DailyQuote? previous)
    {
        foreach (var quote in quotes)
        {
            if (previous is not null && !quote.IsCorrected)
            {
                if (Math.Abs(quote.AdjustedClose / previous.AdjustedClose - 1m) > SpikeThreshold)
                    quote.Flag(QualityFlags.Spike);

                if (BusinessDays.CountAfter(previous.TradingDate, quote.TradingDate.AddDays(-1)) > MaxMissingBusinessDays)
                    quote.Flag(QualityFlags.Gap);
            }

            previous = quote;
        }
    }

    public static bool IsStale(DateOnly lastTradingDate, DateOnly asOf) =>
        BusinessDays.CountAfter(lastTradingDate, asOf) > MaxStaleBusinessDays;
}
