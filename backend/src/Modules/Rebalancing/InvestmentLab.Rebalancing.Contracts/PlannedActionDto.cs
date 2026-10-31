namespace InvestmentLab.Rebalancing.Contracts;

public sealed record PlannedActionDto(
    Guid ActionId,
    string Kind,
    Guid? InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal Amount,
    IReadOnlyList<PlannedAllocationDto> Allocations,
    Guid? FromSubPortfolioId);
