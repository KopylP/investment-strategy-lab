using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Portfolio.Accounts.Infrastructure;

internal sealed class AccountRepository(PortfolioDbContext db) : IAccountRepository
{
    public Task<InvestmentAccount?> FindAsync(AccountId accountId, CancellationToken cancellationToken) =>
        db.Accounts.SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken);

    public async Task<IReadOnlyList<InvestmentAccount>> ListAsync(CancellationToken cancellationToken) =>
        await db.Accounts.OrderBy(account => account.OpenedAt).ToListAsync(cancellationToken);

    public void Add(InvestmentAccount account) => db.Accounts.Add(account);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
