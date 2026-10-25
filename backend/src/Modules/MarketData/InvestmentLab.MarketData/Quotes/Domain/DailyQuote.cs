using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Domain;

/// <summary>
/// One trading day of an instrument's time series. <see cref="Close"/> is the traded price (orders, valuation);
/// <see cref="AdjustedClose"/> includes dividends and splits (strategy signals).
/// </summary>
public sealed class DailyQuote
{
    private DailyQuote() { }

    public InstrumentId InstrumentId { get; private set; }
    public DateOnly TradingDate { get; private set; }
    public decimal Close { get; private set; }
    public decimal AdjustedClose { get; private set; }
    public string Source { get; private set; } = "";
    public DateTimeOffset FetchedAt { get; private set; }
    public QualityFlags QualityFlags { get; private set; }

    public bool IsCorrected => QualityFlags.HasFlag(QualityFlags.Corrected);

    public static DailyQuote Fetched(
        InstrumentId instrumentId, DateOnly tradingDate, decimal close, decimal adjustedClose, string source, DateTimeOffset fetchedAt) =>
        new()
        {
            InstrumentId = instrumentId,
            TradingDate = tradingDate,
            Close = EnsurePositive(close),
            AdjustedClose = EnsurePositive(adjustedClose),
            Source = source,
            FetchedAt = fetchedAt.ToUniversalTime(),
        };

    /// <summary>Takes newly fetched values unless the quote was corrected by hand. Returns whether anything changed.</summary>
    public bool Refresh(decimal close, decimal adjustedClose, string source, DateTimeOffset fetchedAt)
    {
        if (IsCorrected || (close == Close && adjustedClose == AdjustedClose))
            return false;

        (Close, AdjustedClose, Source, FetchedAt) = (EnsurePositive(close), EnsurePositive(adjustedClose), source, fetchedAt.ToUniversalTime());
        return true;
    }

    public void ApplyCorrection(decimal close, decimal adjustedClose)
    {
        (Close, AdjustedClose) = (EnsurePositive(close), EnsurePositive(adjustedClose));
        QualityFlags = QualityFlags.Corrected;
    }

    internal void Flag(QualityFlags flags) => QualityFlags |= flags;

    private static decimal EnsurePositive(decimal price) =>
        price > 0m ? price : throw new ArgumentOutOfRangeException(nameof(price), price, "Quote price must be positive.");
}
