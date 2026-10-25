using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Application.GetHoldings;
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

internal sealed class HoldingsQueries(GetHoldingsHandler getHoldings) : IHoldingsQueries
{
    public async Task<AccountHoldingsDto?> GetHoldings(Guid accountId, DateOnly? asOf, CancellationToken cancellationToken) =>
        await getHoldings.Handle(accountId, asOf, cancellationToken) is { IsSuccess: true } found ? found.Value : null;

    public async Task<SubPortfolioHoldingsDto?> GetSubPortfolioSnapshot(Guid accountId, Guid subPortfolioId, DateOnly? asOf, CancellationToken cancellationToken) =>
        (await GetHoldings(accountId, asOf, cancellationToken))?.SubPortfolios.FirstOrDefault(sub => sub.SubPortfolioId == subPortfolioId);
}
