using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxProgress;

public sealed class GetTaxProgressHandler(TaxRecalculator recalculator, ICurrentUser currentUser, TaxAlertOptions alert)
{
    public async Task<TaxProgressDto> Handle(int year, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        var outcome = await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        var summary = outcome.Computation.Year(year)?.Summary;
        var timeline = TaxCalculator.Timeline(outcome.Events, outcome.Taxpayer.Policy, outcome.Losses, year);

        return new TaxProgressDto(
            year,
            summary?.TaxDue ?? 0m,
            summary?.Gain ?? 0m,
            summary?.DividendIncome ?? 0m,
            outcome.Taxpayer.Taxed.Count,
            timeline,
            alert.Share,
            outcome.Incomplete);
    }
}
