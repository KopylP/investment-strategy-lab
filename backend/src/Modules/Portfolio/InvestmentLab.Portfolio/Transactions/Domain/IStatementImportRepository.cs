using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public interface IStatementImportRepository
{
    Task<StatementImport?> FindAsync(Guid importId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StatementImport>> ForAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    void Add(StatementImport statementImport);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
