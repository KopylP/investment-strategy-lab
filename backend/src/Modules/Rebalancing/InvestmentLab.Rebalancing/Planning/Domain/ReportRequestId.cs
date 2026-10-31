namespace InvestmentLab.Rebalancing.Planning.Domain;

public readonly record struct ReportRequestId
{
    public Guid Value { get; }

    private ReportRequestId(Guid value) => Value = value;

    public static ReportRequestId New() => new(Guid.CreateVersion7());

    public static ReportRequestId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty report request id.", nameof(value));

    public override string ToString() => Value.ToString();
}
