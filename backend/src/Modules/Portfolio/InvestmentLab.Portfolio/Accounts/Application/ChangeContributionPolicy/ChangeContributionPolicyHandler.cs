using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.ChangeContributionPolicy;

public sealed class ChangeContributionPolicyHandler(IAccountRepository accounts)
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, ContributionPolicyInput command, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var policy = AccountInput.ToPolicy(command);
        if (policy.IsFailure)
            return policy.Error;

        account.ChangeContributionPolicy(policy.Value);
        await accounts.SaveChangesAsync(cancellationToken);
        return account.ToSetupDto();
    }
}
