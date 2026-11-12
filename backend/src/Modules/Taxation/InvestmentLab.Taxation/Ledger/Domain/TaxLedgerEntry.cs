namespace InvestmentLab.Taxation.Ledger.Domain;

public sealed class TaxLedgerEntry : IUserOwned
{
    public Guid TransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public TaxableKind Kind { get; private set; }
    public Guid InstrumentId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal Fee { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }
    public List<EntryAllocation> Allocations { get; private set; }
    public bool IsReversed { get; private set; }

    private TaxLedgerEntry(
        Guid transactionId, Guid accountId, UserId ownerId, DateOnly occurredOn, DateTimeOffset recordedAt, TaxableKind kind,
        Guid instrumentId, decimal quantity, decimal price, decimal fee, decimal amount, string currency, IReadOnlyList<EntryAllocation> allocations)
    {
        TransactionId = transactionId;
        AccountId = accountId;
        OwnerId = ownerId;
        OccurredOn = occurredOn;
        RecordedAt = recordedAt;
        Kind = kind;
        InstrumentId = instrumentId;
        Quantity = quantity;
        Price = price;
        Fee = fee;
        Amount = amount;
        Currency = currency;
        Allocations = [.. allocations];
    }

    private TaxLedgerEntry() => (Currency, Allocations) = (null!, null!);

    public static TaxLedgerEntry Trade(
        Guid transactionId, Guid accountId, UserId ownerId, DateOnly occurredOn, DateTimeOffset recordedAt, bool isBuy,
        Guid instrumentId, decimal quantity, decimal price, decimal fee, string currency, IReadOnlyList<EntryAllocation> allocations) =>
        new(transactionId, accountId, ownerId, occurredOn, recordedAt, isBuy ? TaxableKind.Buy : TaxableKind.Sell,
            instrumentId, quantity, price, fee, quantity * price, currency, allocations);

    public static TaxLedgerEntry Dividend(
        Guid transactionId, Guid accountId, UserId ownerId, DateOnly occurredOn, DateTimeOffset recordedAt, Guid instrumentId, decimal amount,
        string currency, IReadOnlyList<EntryAllocation> allocations) =>
        new(transactionId, accountId, ownerId, occurredOn, recordedAt, TaxableKind.Dividend, instrumentId, 0m, 0m, 0m, amount, currency, allocations);

    public void Reverse() => IsReversed = true;

    public void Restate(TaxLedgerEntry current)
    {
        Kind = current.Kind;
        OccurredOn = current.OccurredOn;
        InstrumentId = current.InstrumentId;
        Quantity = current.Quantity;
        Price = current.Price;
        Fee = current.Fee;
        Amount = current.Amount;
        Currency = current.Currency;
        Allocations = current.Allocations;
    }

    public int TaxYear => OccurredOn.Year;
}
