namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ActionDraft(
    ActionKind Kind,
    Guid? InstrumentId,
    decimal PlannedQuantity,
    decimal PlannedPrice,
    IReadOnlyList<ActionAllocation> Allocations,
    string Reason,
    Guid? FromSubPortfolioId = null,
    Guid? ToSubPortfolioId = null);
