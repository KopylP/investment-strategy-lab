namespace InvestmentLab.Rebalancing.Contracts;

public sealed record ReportRequestFailedV1(
    Guid RequestId,
    Guid AccountId,
    Guid OwnerId,
    string Code,
    string Title,
    string Kind,
    string Trigger,
    string Period,
    string Error,
    DateTimeOffset RequestedAt,
    DateTimeOffset FailedAt);
