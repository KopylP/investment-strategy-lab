namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record JournalSnapshotDto(
    Guid AccountId,
    Guid OwnerId,
    string Currency,
    IReadOnlyList<TransactionRecordedV1> Entries,
    IReadOnlyList<Guid> ReversedIds);
