namespace InvestmentLab.Rebalancing.Contracts;

public sealed record ReportClosedV1(Guid ReportId, Guid AccountId, Guid OwnerId, DateTimeOffset ClosedAt);
