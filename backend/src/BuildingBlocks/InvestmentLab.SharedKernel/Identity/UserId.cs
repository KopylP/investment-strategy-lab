namespace InvestmentLab.SharedKernel;

public readonly record struct UserId
{
    public Guid Value { get; }

    private UserId(Guid value) => Value = value;

    public static UserId New() => new(Guid.CreateVersion7());

    public static UserId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty user id.", nameof(value));

    public override string ToString() => Value.ToString();
}
