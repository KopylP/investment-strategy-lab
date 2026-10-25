using InvestmentLab.Portfolio.Accounts.Application.GetAccount;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Infrastructure;

internal sealed class AccountSetupQueries(GetAccountHandler getAccount) : IAccountSetupQueries
{
    public async Task<AccountSetupDto?> GetAccountSetup(Guid accountId, CancellationToken cancellationToken) =>
        await getAccount.Handle(accountId, cancellationToken) is { IsSuccess: true } found ? found.Value : null;

    public Task<IReadOnlyList<AccountSetupDto>> GetAccountSetups(CancellationToken cancellationToken) => getAccount.List(cancellationToken);
}
