using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Application.Shared;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Portfolio.Holdings.Infrastructure;

internal sealed class HoldingsReader(PortfolioDbContext db) : IHoldingsReader
{
    public Task<InvestmentAccount?> FindAccountAsync(AccountId accountId, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken);

    public async Task<AccountHoldings> GetCurrentAsync(InvestmentAccount account, CancellationToken cancellationToken)
    {
        var rows = await db.HoldingRows.AsNoTracking().Where(row => row.AccountId == account.Id.Value).ToListAsync(cancellationToken);

        return rows.Count > 0
            ? HoldingRow.ToHoldings(account, rows)
            : HoldingsProjector.Project(account, []).Value.Holdings;
    }

    public async Task<IReadOnlyList<Transaction>> GetEntriesAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await db.Transactions.AsNoTracking().Where(entry => entry.AccountId == accountId).ToListAsync(cancellationToken);
}
