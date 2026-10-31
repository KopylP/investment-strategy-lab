using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Application;

namespace InvestmentLab.Taxation;

internal sealed class TaxpayerHistory(TaxRecalculator recalculator, ICurrentUser currentUser) : ITaxpayerHistory
{
    public async Task<TaxpayerHistoryDto> LoadAsync(CancellationToken cancellationToken)
    {
        var outcome = await recalculator.RecomputeOwnerAsync(currentUser.RequiredId, cancellationToken);
        var byAccount = outcome.Events.ToLookup(e => e.AccountId);
        var accounts = outcome.Taxpayer.Taxed.Select(account =>
        {
            var setup = outcome.Taxpayer.Setups.GetValueOrDefault(account.AccountId);

            return new TaxpayerAccountHistory(account.AccountId, setup?.Name ?? "", setup?.Broker ?? "", TaxPolicies.For(account.PolicyCode), [.. byAccount[account.AccountId]]);
        });

        return new TaxpayerHistoryDto([.. accounts], outcome.Losses, outcome.Warnings, outcome.Incomplete);
    }
}
