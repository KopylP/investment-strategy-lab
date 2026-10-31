namespace InvestmentLab.Portfolio.Contracts.Transactions;

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
