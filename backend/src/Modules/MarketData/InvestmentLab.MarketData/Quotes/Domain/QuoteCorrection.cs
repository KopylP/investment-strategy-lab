using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Domain;

public readonly record struct QuoteCorrectionId
{
    public Guid Value { get; }

    private QuoteCorrectionId(Guid value) => Value = value;

    public static QuoteCorrectionId New() => new(Guid.CreateVersion7());

    public static QuoteCorrectionId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty quote correction id.", nameof(value));
}

/// <summary>Audit record of a quote fixed by hand: what it became, why and who did it.</summary>
public sealed class QuoteCorrection : AggregateRoot<QuoteCorrectionId>
{
    private QuoteCorrection(QuoteCorrectionId id) : base(id) { }

    public InstrumentId InstrumentId { get; private set; }
    public DateOnly TradingDate { get; private set; }
    public decimal Close { get; private set; }
    public decimal AdjustedClose { get; private set; }
    public string Reason { get; private set; } = "";
    public string CorrectedBy { get; private set; } = "";
    public DateTimeOffset CorrectedAt { get; private set; }

    public static Result<QuoteCorrection> Record(
        InstrumentId instrumentId, DateOnly tradingDate, decimal close, decimal adjustedClose, string reason, string correctedBy, DateTimeOffset correctedAt)
    {
        if (close <= 0m || adjustedClose <= 0m)
            return QuoteErrors.PriceNotPositive();
        if (string.IsNullOrWhiteSpace(reason))
            return QuoteErrors.ReasonRequired();

        return new QuoteCorrection(QuoteCorrectionId.New())
        {
            InstrumentId = instrumentId,
            TradingDate = tradingDate,
            Close = close,
            AdjustedClose = adjustedClose,
            Reason = reason.Trim(),
            CorrectedBy = correctedBy,
            CorrectedAt = correctedAt.ToUniversalTime(),
        };
    }
}
