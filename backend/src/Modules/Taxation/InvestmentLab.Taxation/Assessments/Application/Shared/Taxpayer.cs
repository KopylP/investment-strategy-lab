using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record Taxpayer(UserId OwnerId, IReadOnlyList<TaxAccount> Accounts, IReadOnlyDictionary<Guid, AccountSetupDto> Setups)
{
    public IReadOnlyList<TaxAccount> Taxed { get; } = [.. Accounts.Where(account => TaxPolicies.For(account.PolicyCode).IsTaxed)];

    public ITaxPolicy Policy => Taxed.Count > 0 ? TaxPolicies.For(Taxed[0].PolicyCode) : TaxPolicies.Default;
}
