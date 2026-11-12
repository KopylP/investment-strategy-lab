using InvestmentLab.Rebalancing.Reports.Application.Shared;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReportDetailsDto(
    ReportSummaryDto Summary,
    ContributionPlanDto ContributionPlan,
    IReadOnlyList<ReportActionDto> Actions,
    IReadOnlyList<ReportSectionDto> Sections,
    IReadOnlyList<string> Warnings,
    PlanReviewDto? Review,
    TaxEstimateDto? TaxEstimate,
    IReadOnlyDictionary<Guid, InstrumentLabelDto> Instruments,
    IReadOnlyDictionary<Guid, string> SubPortfolios);
