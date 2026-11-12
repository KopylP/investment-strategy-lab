using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.ListAccounts;

public sealed class ListAccountsHandler(IAccountRepository accounts) : IHandler
{
    public async Task<IReadOnlyList<AccountSetupDto>> Handle(CancellationToken cancellationToken)
    {
        var all = await accounts.ListAsync(cancellationToken);

        return [.. all.Select(account => account.ToSetupDto())];
    }
}
