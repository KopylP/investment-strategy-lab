namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record TargetShare
{
    public Percentage Value { get; }

    private TargetShare(Percentage value) => Value = value;

    public static TargetShare Of(Percentage value) =>
        value.Value > 0m ? new(value) : throw new ArgumentOutOfRangeException(nameof(value), value, "Target share must be above zero.");

    public static Result<TargetShare> TryFromPercent(decimal percent) =>
        percent is > 0m and <= 100m
            ? new TargetShare(Percentage.FromPercent(percent))
            : AccountErrors.TargetShareOutOfRange(percent);

    public override string ToString() => Value.ToString();
}
