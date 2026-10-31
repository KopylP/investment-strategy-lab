namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record ReportRequestFailed(ReportRequest Request, DateTimeOffset OccurredAt) : IDomainEvent;
