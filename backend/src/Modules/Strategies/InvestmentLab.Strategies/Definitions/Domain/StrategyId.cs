namespace InvestmentLab.Strategies.Definitions.Domain;

public readonly record struct StrategyId
{
    public Guid Value { get; }

    private StrategyId(Guid value) => Value = value;

    public static StrategyId New() => new(Guid.CreateVersion7());

    public static StrategyId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty strategy id.", nameof(value));

    public override string ToString() => Value.ToString();
}
