namespace InvestmentLab.Performance.Shadow.Domain;

public sealed record PlannedAction(
    string Kind,
    Guid? InstrumentId,
    decimal Quantity,
    decimal Price,
    IReadOnlyList<PlannedPart> Allocations,
    Guid? FromSubPortfolioId);
