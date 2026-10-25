namespace InvestmentLab.Taxation.Ledger.Domain;

public enum TaxableKind
{
    Buy,
    Sell,
    Dividend,
}

/// <summary>Part of a trade that belongs to one sub-portfolio; the seller is charged with the tax of its part (D8).</summary>
public sealed record EntryAllocation(Guid SubPortfolioId, decimal Quantity);

/// <summary>
/// Taxation's own copy of a journal entry that matters for tax (trade or dividend), taken from Portfolio's events.
/// Amounts are in the account currency; <see cref="IsReversed"/> entries are left out of every computation.
/// </summary>
public sealed class TaxLedgerEntry
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

    /// <summary>Gross amount of a dividend; for trades quantity × price.</summary>
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

    // EF Core.
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

    public int TaxYear => OccurredOn.Year;
}

/// <summary>Account facts Taxation keeps: whose it is, its currency and which tax policy applies.</summary>
public sealed class TaxAccount
{
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Currency { get; private set; }
    public string PolicyCode { get; private set; }

    private TaxAccount(Guid accountId, UserId ownerId, string currency, string policyCode) =>
        (AccountId, OwnerId, Currency, PolicyCode) = (accountId, ownerId, currency, policyCode);

    // EF Core.
    private TaxAccount() => (Currency, PolicyCode) = (null!, null!);

    public static TaxAccount Of(Guid accountId, UserId ownerId, string currency, string policyCode) => new(accountId, ownerId, currency, policyCode);

    public void Update(string currency, string policyCode) => (Currency, PolicyCode) = (currency, policyCode);
}
