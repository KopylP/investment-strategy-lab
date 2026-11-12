namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReportActionDto(
    Guid Id,
    int Sequence,
    string Kind,
    Guid? InstrumentId,
    decimal PlannedQuantity,
    decimal PlannedPrice,
    decimal PlannedAmount,
    IReadOnlyList<ActionAllocationDto> Allocations,
    Guid? FromSubPortfolioId,
    Guid? ToSubPortfolioId,
    string Reason,
    string Status,
    decimal FilledQuantity,
    decimal RemainingQuantity,
    IReadOnlyList<FillDto> Fills,
    string? SkipReason,
    string? Rejection,
    decimal? EstimatedGain,
    decimal? EstimatedTaxChange,
    decimal? EstimatedCarriedLoss);
