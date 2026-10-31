namespace InvestmentLab.Portfolio.Contracts.Transactions;

public interface IJournalQueries
{
    Task<JournalSnapshotDto?> GetJournal(Guid accountId, CancellationToken cancellationToken);
}
