using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxRegime;

public sealed record TaxRegimeDto(
    string TaxCurrency,
    string FxSource,
    string RateLabel,
    string? Summary,
    string? EstimateNote,
    string? TaxedAccountsLabel,
    string? OverviewHint,
    string? ProgressNote,
    string? LossesNote);

public sealed class GetTaxRegimeHandler(IAccountSetupQueries accounts, ICurrentUser currentUser)
{
    public async Task<TaxRegimeDto> Handle(CancellationToken cancellationToken)
    {
        var setups = await accounts.GetAccountSetups(cancellationToken);
        var mine = setups.Where(setup => setup.OwnerId == currentUser.RequiredId.Value);

        var policy = PolicyOf(mine);

        return ToDto(policy);
    }

    private static ITaxPolicy PolicyOf(IEnumerable<AccountSetupDto> accounts)
    {
        var policies = accounts.Select(account => TaxPolicies.For(account.TaxPolicyCode));

        return policies.FirstOrDefault(policy => policy.IsTaxed) ?? TaxPolicies.Default;
    }

    private static TaxRegimeDto ToDto(ITaxPolicy policy) => new(
        TaxCurrency: policy.FxRule.TaxCurrency,
        FxSource: policy.FxRule.SourceName,
        RateLabel: policy.RateSchedule.Label,
        Summary: policy.Texts.Summary,
        EstimateNote: policy.Texts.EstimateNote,
        TaxedAccountsLabel: policy.Texts.TaxedAccountsLabel,
        OverviewHint: policy.Texts.OverviewHint,
        ProgressNote: policy.Texts.ProgressNote,
        LossesNote: policy.Texts.LossesNote);
}
