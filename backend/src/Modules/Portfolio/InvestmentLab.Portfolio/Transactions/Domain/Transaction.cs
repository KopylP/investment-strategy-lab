using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed class Transaction : AggregateRoot<TransactionId>
{
    public AccountId AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public TransactionKind Kind { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public TransactionSource Source { get; private set; }
    public TransactionDetails Details { get; private set; }
    public TransactionId? ReversalOf { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    private Transaction(
        TransactionId id, InvestmentAccount account, TransactionKind kind, DateOnly occurredOn, TransactionSource source,
        TransactionDetails details, DateTimeOffset recordedAt) : base(id)
    {
        AccountId = account.Id;
        OwnerId = account.OwnerId;
        Kind = kind;
        OccurredOn = occurredOn;
        Source = source;
        Details = details;
        ReversalOf = (details as ReversalDetails)?.Original;
        RecordedAt = recordedAt;
    }

    private Transaction(TransactionId id) : base(id) => (Source, Details) = (null!, null!);

    public static Result<Transaction> Trade(
        InvestmentAccount account, TradeSide side, Guid instrumentId, Quantity quantity, Price price, Money fee,
        IReadOnlyList<Allocation> allocations, DateOnly executedOn, TransactionSource source, DateTimeOffset recordedAt, Money? settlement = null)
    {
        if (instrumentId == Guid.Empty)
            return TransactionErrors.InstrumentRequired();

        if (quantity.IsNone || allocations.Any(a => a.Quantity.IsNone))
            return TransactionErrors.QuantityNotPositive();

        if ((CheckCurrency(account, price.Currency) ?? CheckCurrency(account, fee.Currency)) is { } currency)
            return currency;

        if (fee.Amount < 0m)
            return TransactionErrors.FeeNegative();

        if (CheckSettlement(account, settlement) is { } settlementError)
            return settlementError;

        if (CheckSubPortfolios(account, allocations.Select(a => a.SubPortfolioId)) is { } unknown)
            return unknown;

        var allocated = allocations.Sum(a => a.Quantity.Value);
        if (allocated != quantity.Value)
            return TransactionErrors.AllocationsDoNotAddUp(allocated, quantity.Value);

        var kind = side == TradeSide.Buy ? TransactionKind.Buy : TransactionKind.Sell;

        return new Transaction(TransactionId.New(), account, kind, executedOn, source,
            new TradeDetails(instrumentId, side, quantity, price, fee, allocations, settlement), recordedAt);
    }

    public static Result<Transaction> CashMovement(
        InvestmentAccount account, CashMovementKind kind, Money amount, IReadOnlyList<CashAllocation> allocations, Guid? instrumentId,
        DateOnly occurredOn, TransactionSource source, DateTimeOffset recordedAt, Money? settlement = null)
    {
        if (CheckCurrency(account, amount.Currency) is { } currency)
            return currency;

        if (CheckSettlement(account, settlement) is { } settlementError)
            return settlementError;

        if (amount.Amount <= 0m || allocations.Any(a => a.Amount.Amount <= 0m))
            return TransactionErrors.AmountNotPositive();

        if (CheckSubPortfolios(account, allocations.Select(a => a.SubPortfolioId)) is { } unknown)
            return unknown;

        var allocated = allocations.Sum(a => a.Amount.Amount);
        if (allocated != amount.Amount)
            return TransactionErrors.AllocationsDoNotAddUp(allocated, amount.Amount);

        return new Transaction(TransactionId.New(), account, ToKind(kind), occurredOn, source,
            new CashMovementDetails(kind, amount, allocations, instrumentId, settlement), recordedAt);
    }

    public static Result<Transaction> InternalTransfer(
        InvestmentAccount account, SubPortfolioId from, SubPortfolioId to, Guid? instrumentId, Quantity? quantity, Money? amount,
        DateOnly occurredOn, TransactionSource source, DateTimeOffset recordedAt)
    {
        if (from == to)
            return TransactionErrors.TransferToItself();

        if (CheckSubPortfolios(account, [from, to]) is { } unknown)
            return unknown;

        var movesUnits = instrumentId is not null && quantity is { IsNone: false } && amount is null;
        var movesCash = instrumentId is null && quantity is null && amount is { Amount: > 0m };
        if (!movesUnits && !movesCash)
            return TransactionErrors.TransferNeedsInstrumentOrCash();

        if (amount is not null && CheckCurrency(account, amount.Currency) is { } currency)
            return currency;

        return new Transaction(TransactionId.New(), account, TransactionKind.InternalTransfer, occurredOn, source,
            new InternalTransferDetails(from, to, instrumentId, quantity, amount), recordedAt);
    }

    public Result<Transaction> Reverse(InvestmentAccount account, string reason, DateOnly reversedOn, TransactionSource source, DateTimeOffset recordedAt)
    {
        if (Kind == TransactionKind.Reversal)
            return TransactionErrors.CannotReverseReversal();

        if (string.IsNullOrWhiteSpace(reason))
            return TransactionErrors.ReasonRequired();

        return new Transaction(TransactionId.New(), account, TransactionKind.Reversal, reversedOn, source,
            new ReversalDetails(Id, reason.Trim()), recordedAt);
    }

    public void Recorded(Currency accountCurrency, DateTimeOffset now) =>
        Raise(new TransactionRecorded(this, accountCurrency, now));

    public void Reversed(Transaction original, DateTimeOffset now) =>
        Raise(new TransactionReversed(this, original, now));

    private static TransactionKind ToKind(CashMovementKind kind) => kind switch
    {
        CashMovementKind.Contribution => TransactionKind.Contribution,
        CashMovementKind.Withdrawal => TransactionKind.Withdrawal,
        CashMovementKind.Fee => TransactionKind.Fee,
        CashMovementKind.TaxPayment => TransactionKind.TaxPayment,
        CashMovementKind.Dividend => TransactionKind.Dividend,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static Error? CheckCurrency(InvestmentAccount account, Currency currency) =>
        currency == account.BaseCurrency ? null : TransactionErrors.CurrencyMismatch(account.BaseCurrency, currency);

    private static Error? CheckSettlement(InvestmentAccount account, Money? settlement) => settlement switch
    {
        null => null,
        { Amount: <= 0m } => TransactionErrors.AmountNotPositive(),
        _ when settlement.Currency == account.BaseCurrency => TransactionErrors.SettlementInAccountCurrency(account.BaseCurrency),
        _ => null,
    };

    private static Error? CheckSubPortfolios(InvestmentAccount account, IEnumerable<SubPortfolioId> ids)
    {
        var list = ids.ToList();
        if (list.Distinct().Count() != list.Count)
            return TransactionErrors.AllocationRepeated();

        foreach (var id in list)
        {
            if (!account.Has(id))
                return TransactionErrors.UnknownSubPortfolio(id.Value);
        }

        return null;
    }
}
