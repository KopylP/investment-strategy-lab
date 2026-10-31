namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record TradeMarked(
    Guid AccountId, string Reference, Guid InstrumentId, bool IsSale, decimal Quantity, decimal Price, decimal Fee, DateOnly ExecutedOn,
    IReadOnlyList<ActionAllocation> Allocations, DateTimeOffset OccurredAt) : IDomainEvent;
