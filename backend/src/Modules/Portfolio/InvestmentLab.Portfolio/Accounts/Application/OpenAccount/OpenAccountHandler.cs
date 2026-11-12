using InvestmentLab.SharedKernel;
using InvestmentLab.Portfolio.Accounts.Application.Shared;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Portfolio.Accounts.Application.OpenAccount;

public sealed record OpenAccountCommand(
    string Name,
    string Broker,
    string BaseCurrency,
    IReadOnlyList<NewSubPortfolioDto> SubPortfolios,
    ContributionPolicyChoice? ContributionPolicy = null,
    string? TaxPolicyCode = null);

public sealed class OpenAccountHandler(IAccountRepository accounts, ITaxPolicyDirectory taxPolicies, TimeProvider clock) : IHandler
{
    public async Task<Result<AccountSetupDto>> Handle(UserId ownerId, OpenAccountCommand command, CancellationToken cancellationToken)
    {
        var currency = Currency.TryOf(command.BaseCurrency);
        if (currency.IsFailure)
            return currency.Error;

        var taxPolicy = string.IsNullOrWhiteSpace(command.TaxPolicyCode) ? taxPolicies.DefaultCode : command.TaxPolicyCode;
        if (!taxPolicies.IsKnown(taxPolicy))
            return AccountErrors.UnknownTaxPolicy(taxPolicy);

        var policy = PolicyOf(command.ContributionPolicy);
        if (policy.IsFailure)
            return policy.Error;

        var drafts = new List<SubPortfolioDraft>();

        foreach (var asked in command.SubPortfolios ?? [])
        {
            var draft = SubPortfolioDraft.TryOf(asked.ToDomain());
            if (draft.IsFailure)
                return draft.Error;

            drafts.Add(draft.Value);
        }

        var now = clock.GetUtcNow();
        var opened = InvestmentAccount.Open(
            ownerId,
            command.Name ?? "",
            command.Broker ?? "",
            currency.Value,
            taxPolicy,
            policy.Value,
            drafts,
            now);
        if (opened.IsFailure)
            return opened.Error;

        var account = opened.Value;
        accounts.Add(account);
        await accounts.SaveChangesAsync(cancellationToken);

        return account.ToSetupDto();
    }

    private static Result<ContributionPolicy> PolicyOf(ContributionPolicyChoice? choice) =>
        choice is null ? ContributionPolicy.Default : ContributionPolicy.TryOf(choice.Kind, choice.DriftBandPercent);
}
