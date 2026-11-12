using InvestmentLab.SharedKernel;
using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.ChangeContributionPolicy;

public sealed class ChangeContributionPolicyHandler(IAccountRepository accounts) : IHandler
{
    public async Task<Result<AccountSetupDto>> Handle(Guid accountId, ContributionPolicyChoice choice, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await accounts.FindAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var policy = ContributionPolicy.TryOf(choice.Kind, choice.DriftBandPercent);
        if (policy.IsFailure)
            return policy.Error;

        account.ChangeContributionPolicy(policy.Value);
        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }
}
