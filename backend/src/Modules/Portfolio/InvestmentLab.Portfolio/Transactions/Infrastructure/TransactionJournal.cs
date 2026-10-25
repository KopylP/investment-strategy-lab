using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Application;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class TransactionJournal(PortfolioDbContext db) : ITransactionJournal
{
    public Task<InvestmentAccount?> FindAccountAsync(AccountId accountId, CancellationToken cancellationToken) =>
        db.Accounts.SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetEntriesAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await db.Transactions.Where(entry => entry.AccountId == accountId).ToListAsync(cancellationToken);

    public Task<bool> SourceExistsAsync(string sourceReference, CancellationToken cancellationToken) =>
        db.Transactions.IgnoreQueryFilters([ModuleDbContext.OwnerFilter]).AnyAsync(entry => entry.Source.Reference == sourceReference, cancellationToken);

    public void Add(Transaction transaction) => db.Transactions.Add(transaction);

    public async Task ReplaceHoldingsAsync(AccountHoldings holdings, UserId ownerId, CancellationToken cancellationToken)
    {
        var accountId = holdings.AccountId.Value;
        db.HoldingRows.RemoveRange(await db.HoldingRows.Where(row => row.AccountId == accountId).ToListAsync(cancellationToken));
        db.HoldingRows.AddRange(HoldingRow.From(holdings, ownerId));
    }

    public void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class => db.Publish(integrationEvent, occurredAt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
