using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

internal static class TaxAccountMapping
{
    public static IReadOnlyList<TaxAccountDto> AccountsOf(Taxpayer taxpayer) =>
        [.. taxpayer.Accounts.Select(account => ToDto(account, taxpayer))];

    private static TaxAccountDto ToDto(TaxAccount account, Taxpayer taxpayer)
    {
        var setup = taxpayer.Setups.GetValueOrDefault(account.AccountId);
        var isTaxed = TaxPolicies.For(account.PolicyCode).IsTaxed;

        return new TaxAccountDto(account.AccountId, setup?.Name ?? "", setup?.Broker ?? "", account.Currency, account.PolicyCode, isTaxed);
    }
}
