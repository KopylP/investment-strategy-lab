namespace InvestmentLab.SharedKernel;

public sealed record Percentage : IComparable<Percentage>
{
    public static readonly Percentage Zero = new(0m);
    public static readonly Percentage Full = new(1m);

    public decimal Value { get; }

    private Percentage(decimal value) => Value = value;

    public static Percentage Of(decimal fraction) =>
        fraction is >= 0m and <= 1m
            ? new(fraction)
            : throw new ArgumentOutOfRangeException(nameof(fraction), fraction, "Expected a fraction between 0 and 1.");

    public static Result<Percentage> TryOf(decimal fraction) =>
        fraction is >= 0m and <= 1m
            ? new Percentage(fraction)
            : Error.Validation("percentage.out_of_range", $"{fraction} is not between 0 and 1.");

    public static Percentage FromPercent(decimal percent) => Of(percent / 100m);

    public static Result<Percentage?> TryFromOptionalPercent(decimal? percent)
    {
        if (percent is not { } value)
            return Result<Percentage?>.Success(null);

        var fraction = TryOf(value / 100m);

        return fraction.IsFailure ? fraction.Error : fraction.Value;
    }

    public Money Of(Money amount) => amount.Multiply(Value);

    public int CompareTo(Percentage? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override string ToString() => $"{Value * 100m:0.##}%";
}
