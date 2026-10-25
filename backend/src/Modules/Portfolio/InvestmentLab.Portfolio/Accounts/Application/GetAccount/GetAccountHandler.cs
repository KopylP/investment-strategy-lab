using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.GetAccount;

public sealed class GetAccountHandler(IAccountRepository accounts)
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, CancellationToken cancellationToken) =>
        accountId != Guid.Empty && await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is { } account
            ? account.ToSetupDto()
            : AccountErrors.NotFound(accountId);

    public async Task<IReadOnlyList<AccountSetupDto>> List(CancellationToken cancellationToken) =>
        [.. (await accounts.ListAsync(cancellationToken)).Select(account => account.ToSetupDto())];
}
