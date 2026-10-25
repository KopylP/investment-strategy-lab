namespace InvestmentLab.Portfolio.Transactions.Domain;

public readonly record struct TransactionId
{
    public Guid Value { get; }

    private TransactionId(Guid value) => Value = value;

    public static TransactionId New() => new(Guid.CreateVersion7());

    public static TransactionId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty transaction id.", nameof(value));

    public override string ToString() => Value.ToString();
}
