namespace InvestmentLab.Rebalancing.Reports.Domain;

public readonly record struct ReportActionId
{
    public Guid Value { get; }

    private ReportActionId(Guid value) => Value = value;

    public static ReportActionId New() => new(Guid.CreateVersion7());

    public static ReportActionId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty report action id.", nameof(value));

    public override string ToString() => Value.ToString();
}
