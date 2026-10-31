namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ReportGenerated(RebalancingReport Report, DateTimeOffset RequestedAt, DateTimeOffset OccurredAt) : IDomainEvent;
