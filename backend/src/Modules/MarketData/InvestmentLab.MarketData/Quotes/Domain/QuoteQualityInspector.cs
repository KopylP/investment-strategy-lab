namespace InvestmentLab.MarketData.Quotes.Domain;

/// <summary>Flags suspicious quotes so reports can warn before acting on them.</summary>
public static class QuoteQualityInspector
{
    public const decimal SpikeThreshold = 0.20m;
    public const int MaxMissingBusinessDays = 5;
    public const int MaxStaleBusinessDays = 5;

    /// <summary>Flags spikes and gaps in <paramref name="quotes"/> (oldest first); the first is compared with <paramref name="previous"/>.</summary>
    public static void Inspect(IReadOnlyList<DailyQuote> quotes, DailyQuote? previous)
    {
        foreach (var quote in quotes)
        {
            if (previous is not null && !quote.IsCorrected)
            {
                if (Math.Abs(quote.AdjustedClose / previous.AdjustedClose - 1m) > SpikeThreshold)
                    quote.Flag(QualityFlags.Spike);

                // Business days strictly between the two quotes.
                if (BusinessDays.CountAfter(previous.TradingDate, quote.TradingDate.AddDays(-1)) > MaxMissingBusinessDays)
                    quote.Flag(QualityFlags.Gap);
            }

            previous = quote;
        }
    }

    public static bool IsStale(DateOnly lastTradingDate, DateOnly asOf) =>
        BusinessDays.CountAfter(lastTradingDate, asOf) > MaxStaleBusinessDays;
}
