using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed class StatementImportLoader(ITransactionJournal journal, IStatementImportRepository imports)
{
    public async Task<Result<(InvestmentAccount Account, StatementImport Import)>> LoadAsync(
        Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await journal.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        if (await imports.FindAsync(importId, cancellationToken) is not { } statement || statement.AccountId != account.Id)
            return StatementImportErrors.NotFound(importId);

        return (account, statement);
    }
}
