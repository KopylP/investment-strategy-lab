using InvestmentLab.Taxation.Ledger.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Taxation.Ledger.Infrastructure;

internal sealed class TaxLedgerRepository(TaxationDbContext db) : ITaxLedgerRepository
{
    public Task<TaxAccount?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        db.Accounts.SingleOrDefaultAsync(account => account.AccountId == accountId, cancellationToken);

    public async Task<IReadOnlyList<TaxLedgerEntry>> EntriesAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Entries.Where(entry => entry.AccountId == accountId).ToListAsync(cancellationToken);

    public Task<TaxLedgerEntry?> FindEntryAsync(Guid transactionId, CancellationToken cancellationToken) =>
        db.Entries.SingleOrDefaultAsync(entry => entry.TransactionId == transactionId, cancellationToken);

    public void AddAccount(TaxAccount account) => db.Accounts.Add(account);

    public void AddEntry(TaxLedgerEntry entry) => db.Entries.Add(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
