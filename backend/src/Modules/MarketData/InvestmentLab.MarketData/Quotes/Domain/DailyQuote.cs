using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Domain;

public sealed class DailyQuote
{
    private DailyQuote() { }

    public InstrumentId InstrumentId { get; private set; }
    public DateOnly TradingDate { get; private set; }
    public decimal? Open { get; private set; }
    public decimal? High { get; private set; }
    public decimal? Low { get; private set; }
    public decimal Close { get; private set; }
    public decimal AdjustedClose { get; private set; }
    public string Source { get; private set; } = "";
    public DateTimeOffset FetchedAt { get; private set; }
    public QualityFlags QualityFlags { get; private set; }

    public bool IsCorrected => QualityFlags.HasFlag(QualityFlags.Corrected);

    public DayRange? Range => Open is { } open && High is { } high && Low is { } low ? new DayRange(open, high, low) : null;

    public static DailyQuote Fetched(
        InstrumentId instrumentId, DateOnly tradingDate, decimal close, decimal adjustedClose, DayRange? range, string source, DateTimeOffset fetchedAt) =>
        new()
        {
            InstrumentId = instrumentId,
            TradingDate = tradingDate,
            Open = range?.Open,
            High = range?.High,
            Low = range?.Low,
            Close = EnsurePositive(close),
            AdjustedClose = EnsurePositive(adjustedClose),
            Source = source,
            FetchedAt = fetchedAt.ToUniversalTime(),
        };

    public bool Refresh(decimal close, decimal adjustedClose, string source, DateTimeOffset fetchedAt)
    {
        if (IsCorrected || (close == Close && adjustedClose == AdjustedClose))
            return false;

        (Close, AdjustedClose, Source, FetchedAt) = (EnsurePositive(close), EnsurePositive(adjustedClose), source, fetchedAt.ToUniversalTime());
        ClearRange();

        return true;
    }

    public void UseRange(DayRange? range)
    {
        if (IsCorrected || range is not { } known)
            return;

        (Open, High, Low) = (known.Open, known.High, known.Low);
    }

    public void ApplyCorrection(decimal close, decimal adjustedClose)
    {
        if (close != Close)
            ClearRange();

        (Close, AdjustedClose) = (EnsurePositive(close), EnsurePositive(adjustedClose));
        QualityFlags = QualityFlags.Corrected;
    }

    internal void Flag(QualityFlags flags) => QualityFlags |= flags;

    private void ClearRange() => (Open, High, Low) = (null, null, null);

    private static decimal EnsurePositive(decimal price) =>
        price > 0m ? price : throw new ArgumentOutOfRangeException(nameof(price), price, "Quote price must be positive.");
}
