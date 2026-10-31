namespace InvestmentLab.Rebalancing.Contracts;

public sealed record ReportGeneratedV1(
    Guid ReportId,
    Guid AccountId,
    Guid OwnerId,
    string Period,
    string Kind,
    DateOnly MarketAsOf,
    string Currency,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<PlannedActionDto> Actions,
    string? AccountName = null,
    string? Trigger = null,
    string? Title = null,
    DateTimeOffset? RequestedAt = null,
    string? Code = null);
