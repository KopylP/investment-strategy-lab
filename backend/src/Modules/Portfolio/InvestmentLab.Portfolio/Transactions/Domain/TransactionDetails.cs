using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public enum TransactionKind
{
    Buy,
    Sell,
    Contribution,
    Withdrawal,
    Fee,
    TaxPayment,
    Dividend,
    InternalTransfer,
    Reversal,
}

public enum TradeSide
{
    Buy,
    Sell,
}

/// <summary>Cash that enters or leaves the account without a trade.</summary>
public enum CashMovementKind
{
    Contribution,
    Withdrawal,
    Fee,
    TaxPayment,
    Dividend,
}

public enum TransactionSourceKind
{
    /// <summary>A fill marked in a rebalancing report.</summary>
    ReportAction,
    Manual,
    Reconciliation,
    Import,
}

/// <param name="Reference">Idempotency key of the origin (e.g. <c>actionId:fillNo</c>); unique per account when present.</param>
public sealed record TransactionSource(TransactionSourceKind Kind, string? Reference)
{
    public static TransactionSource Manual => new(TransactionSourceKind.Manual, null);
}

/// <summary>Part of a broker trade that belongs to one sub-portfolio.</summary>
public sealed record Allocation(SubPortfolioId SubPortfolioId, Quantity Quantity);

/// <summary>Part of a cash movement that belongs to one sub-portfolio.</summary>
public sealed record CashAllocation(SubPortfolioId SubPortfolioId, Money Amount);

/// <summary>Body of a transaction; exactly one variant per kind.</summary>
public abstract record TransactionDetails;

/// <param name="Fee">Broker fee of the whole trade; split between sub-portfolios like the quantity.</param>
public sealed record TradeDetails(Guid InstrumentId, TradeSide Side, Quantity Quantity, Price Price, Money Fee, IReadOnlyList<Allocation> Allocations)
    : TransactionDetails;

/// <param name="Amount">Always positive; the kind says whether cash comes in or goes out.</param>
/// <param name="InstrumentId">Instrument that paid a dividend, if any.</param>
public sealed record CashMovementDetails(CashMovementKind Kind, Money Amount, IReadOnlyList<CashAllocation> Allocations, Guid? InstrumentId)
    : TransactionDetails
{
    public bool IsInflow => Kind is CashMovementKind.Contribution or CashMovementKind.Dividend;
}

/// <summary>Moves units of an instrument (with their lots) or cash between two sub-portfolios.</summary>
public sealed record InternalTransferDetails(SubPortfolioId From, SubPortfolioId To, Guid? InstrumentId, Quantity? Quantity, Money? Amount)
    : TransactionDetails;

public sealed record ReversalDetails(TransactionId Original, string Reason) : TransactionDetails;
