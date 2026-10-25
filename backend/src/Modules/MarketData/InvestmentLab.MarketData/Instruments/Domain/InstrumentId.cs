namespace InvestmentLab.MarketData.Instruments.Domain;

public readonly record struct InstrumentId
{
    public Guid Value { get; }

    private InstrumentId(Guid value) => Value = value;

    public static InstrumentId New() => new(Guid.CreateVersion7());

    public static InstrumentId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty instrument id.", nameof(value));

    public override string ToString() => Value.ToString();
}
