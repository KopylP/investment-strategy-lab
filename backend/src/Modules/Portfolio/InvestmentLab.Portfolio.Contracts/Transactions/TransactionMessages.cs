namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record TransactionRecordedV1(
    Guid TransactionId,
    Guid AccountId,
    Guid OwnerId,
    string Kind,
    DateOnly OccurredOn,
    string Source,
    string? SourceReference,
    string Currency,
    TradeRecordedDto? Trade,
    CashMovementRecordedDto? Cash,
    InternalTransferRecordedDto? Transfer);
