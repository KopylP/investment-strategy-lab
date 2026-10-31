using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReportDetailsDto(
    ReportSummaryDto Summary,
    ContributionPlan ContributionPlan,
    IReadOnlyList<ReportActionDto> Actions,
    IReadOnlyList<ReportSection> Sections,
    IReadOnlyList<string> Warnings,
    PlanReview? Review,
    TaxEstimate? TaxEstimate,
    IReadOnlyDictionary<Guid, InstrumentLabelDto> Instruments,
    IReadOnlyDictionary<Guid, string> SubPortfolios);
