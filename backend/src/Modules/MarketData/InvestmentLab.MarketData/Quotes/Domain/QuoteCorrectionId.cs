namespace InvestmentLab.MarketData.Quotes.Domain;

public readonly record struct QuoteCorrectionId
{
    public Guid Value { get; }

    private QuoteCorrectionId(Guid value) => Value = value;

    public static QuoteCorrectionId New() => new(Guid.CreateVersion7());

    public static QuoteCorrectionId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty quote correction id.", nameof(value));
}
