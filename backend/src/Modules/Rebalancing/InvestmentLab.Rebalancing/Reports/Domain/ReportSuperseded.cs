namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ReportSuperseded(ReportId ReportId, Guid AccountId, ReportId SupersededBy, DateTimeOffset OccurredAt) : IDomainEvent;
