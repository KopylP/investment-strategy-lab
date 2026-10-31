using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Shared.Infrastructure;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Portfolio.Transactions.Infrastructure;

internal sealed class StatementImportRepository(PortfolioDbContext db) : IStatementImportRepository
{
    public Task<StatementImport?> FindAsync(Guid importId, CancellationToken cancellationToken) =>
        db.StatementImports.SingleOrDefaultAsync(statement => statement.Id == importId, cancellationToken);

    public async Task<IReadOnlyList<StatementImport>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken) =>
        await db.StatementImports.Where(statement => statement.AccountId == accountId).ToListAsync(cancellationToken);

    public void Add(StatementImport statementImport) => db.StatementImports.Add(statementImport);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
