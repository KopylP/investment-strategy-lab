namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record QuantityAllocationDto(Guid SubPortfolioId, decimal Quantity);

public sealed record AmountAllocationDto(Guid SubPortfolioId, decimal Amount);

/// <param name="Side">Buy or Sell.</param>
public sealed record TradeRecordedDto(Guid InstrumentId, string Side, decimal Quantity, decimal Price, decimal Fee, IReadOnlyList<QuantityAllocationDto> Allocations);

/// <param name="Kind">Contribution, Withdrawal, Fee, TaxPayment or Dividend; <paramref name="Amount"/> is always positive.</param>
public sealed record CashMovementRecordedDto(string Kind, decimal Amount, Guid? InstrumentId, IReadOnlyList<AmountAllocationDto> Allocations);

public sealed record InternalTransferRecordedDto(Guid FromSubPortfolioId, Guid ToSubPortfolioId, Guid? InstrumentId, decimal? Quantity, decimal? Amount);

/// <summary>A journal entry was recorded; exactly one of trade, cash or transfer is set.</summary>
/// <param name="Source">ReportAction, Manual, Reconciliation or Import.</param>
public sealed record TransactionRecordedV1(
    Guid TransactionId,
    Guid AccountId,
    Guid OwnerId,
    string Kind,
    DateOnly OccurredOn,
    string Source,
    string? SourceReference,
    string Currency,
    TradeRecordedDto? Trade,
    CashMovementRecordedDto? Cash,
    InternalTransferRecordedDto? Transfer);

/// <summary>An entry was undone; anything computed from it is outdated from <paramref name="OriginalOccurredOn"/>.</summary>
public sealed record TransactionReversedV1(
    Guid TransactionId,
    Guid ReversalId,
    Guid AccountId,
    Guid OwnerId,
    DateOnly OriginalOccurredOn,
    string? SourceReference,
    string Reason);

/// <summary>A command from another module could not be recorded; the sender should undo its side.</summary>
public sealed record TransactionRejectedV1(string SourceReference, string Code, string Reason);

/// <summary>Command: record a broker trade, e.g. a fill marked in a rebalancing report. Idempotent by <paramref name="SourceReference"/>.</summary>
/// <param name="Allocations">Null: split by target shares (buy) or by held units (sell).</param>
public sealed record RecordTradeV1(
    Guid AccountId,
    string SourceReference,
    Guid InstrumentId,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    DateOnly ExecutedOn,
    IReadOnlyList<QuantityAllocationDto>? Allocations);

/// <summary>Command: record money paid into the account. Idempotent by <paramref name="SourceReference"/>.</summary>
public sealed record RecordContributionV1(
    Guid AccountId,
    string SourceReference,
    decimal Amount,
    DateOnly OccurredOn,
    IReadOnlyList<AmountAllocationDto>? Allocations);

/// <summary>Command: undo the entry recorded from <paramref name="SourceReference"/> (e.g. an unmarked fill).</summary>
public sealed record ReverseTransactionV1(Guid AccountId, string SourceReference, string Reason);
