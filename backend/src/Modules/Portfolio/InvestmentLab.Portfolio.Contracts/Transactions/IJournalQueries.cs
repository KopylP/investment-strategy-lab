namespace InvestmentLab.Portfolio.Contracts.Transactions;

/// <summary>The journal as events, for modules that keep their own copy (Taxation, Performance) and need to catch up.</summary>
public interface IJournalQueries
{
    /// <summary>Every entry of the account as it was published, oldest first; reversed entries are listed in <see cref="JournalSnapshotDto.ReversedIds"/>.</summary>
    Task<JournalSnapshotDto?> GetJournal(Guid accountId, CancellationToken cancellationToken);
}

public sealed record JournalSnapshotDto(Guid AccountId, Guid OwnerId, string Currency, IReadOnlyList<TransactionRecordedV1> Entries, IReadOnlyList<Guid> ReversedIds);
