namespace InvestmentLab.SharedKernel;

public sealed record Quantity : IComparable<Quantity>
{
    public static readonly Quantity None = new(0m);

    public decimal Value { get; }

    private Quantity(decimal value) => Value = value;

    public static Quantity Of(decimal value) =>
        value >= 0m ? new(value) : throw new ArgumentOutOfRangeException(nameof(value), value, "Quantity cannot be negative.");

    public static Result<Quantity> TryOf(decimal value) =>
        value >= 0m ? new Quantity(value) : Error.Validation("quantity.negative", $"Quantity {value} cannot be negative.");

    public bool IsNone => Value == 0m;

    public static Quantity operator +(Quantity left, Quantity right) => new(left.Value + right.Value);
    public static Quantity operator -(Quantity left, Quantity right) => Of(left.Value - right.Value);

    public static bool operator >(Quantity left, Quantity right) => left.Value > right.Value;
    public static bool operator <(Quantity left, Quantity right) => left.Value < right.Value;
    public static bool operator >=(Quantity left, Quantity right) => left.Value >= right.Value;
    public static bool operator <=(Quantity left, Quantity right) => left.Value <= right.Value;

    public int CompareTo(Quantity? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
