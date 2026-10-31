namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record MarkUndone(Guid AccountId, string Reference, DateTimeOffset OccurredAt) : IDomainEvent;
