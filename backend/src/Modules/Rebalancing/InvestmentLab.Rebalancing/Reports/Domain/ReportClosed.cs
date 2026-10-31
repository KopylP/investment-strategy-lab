namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ReportClosed(ReportId ReportId, Guid AccountId, UserId OwnerId, DateTimeOffset OccurredAt) : IDomainEvent;
