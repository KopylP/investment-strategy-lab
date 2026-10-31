namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReportSummaryDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    string Period,
    string Kind,
    string Code,
    string Title,
    string Trigger,
    string Status,
    DateOnly MarketAsOf,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? ClosedAt,
    string Currency,
    decimal Contribution,
    int ActionsTotal,
    int ActionsOpen,
    Guid? SupersededBy);
