namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record ContributionMarked(
    Guid AccountId, string Reference, decimal Amount, DateOnly ExecutedOn, IReadOnlyList<ActionAllocation> Allocations, DateTimeOffset OccurredAt) : IDomainEvent;
