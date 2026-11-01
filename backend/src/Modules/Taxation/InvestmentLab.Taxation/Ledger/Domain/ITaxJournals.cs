namespace InvestmentLab.Taxation.Ledger.Domain;

public interface ITaxJournals
{
    Task<TaxJournal?> FindAsync(Guid accountId, CancellationToken cancellationToken);
}
