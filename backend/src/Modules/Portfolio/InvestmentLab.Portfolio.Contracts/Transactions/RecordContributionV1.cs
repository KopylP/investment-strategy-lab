namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record RecordContributionV1(
    Guid AccountId,
    string SourceReference,
    decimal Amount,
    DateOnly OccurredOn,
    IReadOnlyList<AmountAllocationDto>? Allocations);
