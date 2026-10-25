namespace InvestmentLab.SharedKernel;

/// <summary>A share between 0 and 1 (weights, target shares, tax rates).</summary>
public sealed record Percentage : IComparable<Percentage>
{
    public static readonly Percentage Zero = new(0m);
    public static readonly Percentage Full = new(1m);

    public decimal Value { get; }

    private Percentage(decimal value) => Value = value;

    public static Percentage Of(decimal fraction) =>
        fraction is >= 0m and <= 1m ? new(fraction) : throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Expected a fraction between 0 and 1.");

    public static Result<Percentage> TryOf(decimal fraction) =>
        fraction is >= 0m and <= 1m
            ? new Percentage(fraction)
            : Error.Validation("percentage.out_of_range", $"{fraction} is not between 0 and 1.");

    public static Percentage FromPercent(decimal percent) => Of(percent / 100m);

    public Money Of(Money amount) => amount.Multiply(Value);

    public int CompareTo(Percentage? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override string ToString() => $"{Value * 100m:0.##}%";
}

/// <summary>A fraction that may be negative: returns, drawdowns, changes.</summary>
public sealed record SignedPercentage
{
    public decimal Value { get; }

    private SignedPercentage(decimal value) => Value = value;

    public static SignedPercentage Of(decimal fraction) => new(fraction);
    public static SignedPercentage FromPercent(decimal percent) => new(percent / 100m);

    public bool IsNegative => Value < 0m;

    public override string ToString() => $"{Value * 100m:+0.##;-0.##;0}%";
}
