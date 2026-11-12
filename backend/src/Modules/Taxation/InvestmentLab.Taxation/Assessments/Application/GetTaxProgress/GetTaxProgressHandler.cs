using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxProgress;

public sealed class GetTaxProgressHandler(TaxAlertOptions alert, TaxRecalculator recalculator, ICurrentUser currentUser)
{
    public async Task<TaxProgressDto> Handle(int year, CancellationToken cancellationToken)
    {
        var refreshed = await recalculator.RefreshedAsync(currentUser.RequiredId, cancellationToken);
        var summary = refreshed.Computation.Year(year)?.Summary;
        var timeline = TaxCalculator.Timeline(refreshed.Events, refreshed.Taxpayer.Policy, refreshed.Losses, year);

        return new TaxProgressDto(
            year,
            summary?.TaxDue ?? 0m,
            summary?.Gain ?? 0m,
            summary?.DividendIncome ?? 0m,
            refreshed.Taxpayer.Taxed.Count,
            [.. timeline.Select(TaxTimelinePointDto.From)],
            alert.Share,
            refreshed.Incomplete);
    }
}
