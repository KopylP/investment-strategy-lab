using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ReportDraft(
    Guid AccountId,
    UserId OwnerId,
    string AccountName,
    Currency Currency,
    YearMonth Period,
    ReportKind Kind,
    ReportTrigger Trigger,
    DateOnly MarketAsOf,
    ContributionPlan Contribution,
    IReadOnlyList<ReportSection> Sections,
    IReadOnlyList<ActionDraft> Actions,
    IReadOnlyList<string> Warnings,
    PlanReview? Review,
    string? Code = null,
    string? Title = null);
