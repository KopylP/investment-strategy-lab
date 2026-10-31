using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReportActionDto(
    Guid Id,
    int Sequence,
    string Kind,
    Guid? InstrumentId,
    decimal PlannedQuantity,
    decimal PlannedPrice,
    decimal PlannedAmount,
    IReadOnlyList<ActionAllocation> Allocations,
    Guid? FromSubPortfolioId,
    Guid? ToSubPortfolioId,
    string Reason,
    string Status,
    decimal FilledQuantity,
    decimal RemainingQuantity,
    IReadOnlyList<Fill> Fills,
    string? SkipReason,
    string? Rejection,
    decimal? EstimatedGainPln,
    decimal? EstimatedTaxPln);
