namespace InvestmentLab.Taxation.Ledger.Domain;

public interface ITaxLedgerRepository
{
    Task<TaxAccount?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaxLedgerEntry>> EntriesAsync(Guid accountId, CancellationToken cancellationToken);

    Task<TaxLedgerEntry?> FindEntryAsync(Guid transactionId, CancellationToken cancellationToken);

    void AddAccount(TaxAccount account);

    void AddEntry(TaxLedgerEntry entry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
