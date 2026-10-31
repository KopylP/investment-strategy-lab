namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record TransactionReversedV1(
    Guid TransactionId,
    Guid ReversalId,
    Guid AccountId,
    Guid OwnerId,
    DateOnly OriginalOccurredOn,
    string? SourceReference,
    string Reason);
