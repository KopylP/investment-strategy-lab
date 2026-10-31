using InvestmentLab.Portfolio.Accounts.Application.GetAccount;
using InvestmentLab.Portfolio.Accounts.Application.ListAccounts;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Infrastructure;

internal sealed class AccountSetupQueries(GetAccountHandler getAccount, ListAccountsHandler listAccounts) : IAccountSetupQueries
{
    public async Task<AccountSetupDto?> GetAccountSetup(Guid accountId, CancellationToken cancellationToken)
    {
        var found = await getAccount.Handle(accountId, cancellationToken);

        return found.IsSuccess ? found.Value : null;
    }

    public Task<IReadOnlyList<AccountSetupDto>> GetAccountSetups(CancellationToken cancellationToken) => listAccounts.Handle(cancellationToken);
}
