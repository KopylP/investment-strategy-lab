using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.OpenAccount;

public sealed record OpenAccountCommand(
    string Name,
    string Broker,
    string BaseCurrency,
    IReadOnlyList<SubPortfolioInput> SubPortfolios,
    ContributionPolicyInput? ContributionPolicy = null,
    string? TaxPolicyCode = null);

public sealed class OpenAccountHandler(IAccountRepository accounts, TimeProvider clock)
{
    public async Task<Result<AccountSetupDto>> Handle(UserId ownerId, OpenAccountCommand command, CancellationToken cancellationToken)
    {
        var currency = Currency.TryOf(command.BaseCurrency);
        if (currency.IsFailure)
            return currency.Error;

        var policy = AccountInput.ToPolicy(command.ContributionPolicy);
        if (policy.IsFailure)
            return policy.Error;

        var drafts = new List<SubPortfolioDraft>();

        foreach (var input in command.SubPortfolios ?? [])
        {
            var draft = AccountInput.ToDraft(input);
            if (draft.IsFailure)
                return draft.Error;

            drafts.Add(draft.Value);
        }

        var now = clock.GetUtcNow();
        var opened = InvestmentAccount.Open(ownerId, command.Name ?? "", command.Broker ?? "", currency.Value, command.TaxPolicyCode, policy.Value, drafts, now);
        if (opened.IsFailure)
            return opened.Error;

        var account = opened.Value;
        accounts.Add(account);
        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }
}
