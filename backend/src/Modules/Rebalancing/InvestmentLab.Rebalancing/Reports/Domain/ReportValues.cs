namespace InvestmentLab.Rebalancing.Reports.Domain;

public readonly record struct ReportId
{
    public Guid Value { get; }

    private ReportId(Guid value) => Value = value;

    public static ReportId New() => new(Guid.CreateVersion7());

    public static ReportId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty report id.", nameof(value));

    public override string ToString() => Value.ToString();
}
