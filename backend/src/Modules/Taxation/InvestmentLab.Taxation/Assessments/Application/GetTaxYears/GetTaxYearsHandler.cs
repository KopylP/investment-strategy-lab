using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxYears;

public sealed class GetTaxYearsHandler(IAssessmentRepository assessments, TaxRecalculator recalculator, ICurrentUser currentUser)
{
    public async Task<TaxOverviewDto> Handle(CancellationToken cancellationToken)
    {
        var refreshed = await recalculator.RefreshedAsync(currentUser.RequiredId, cancellationToken);
        var assessed = await assessments.ForOwnerAsync(refreshed.Taxpayer.OwnerId, cancellationToken);

        var years = assessed
            .OrderByDescending(assessment => assessment.Year)
            .Select(assessment => new TaxYearListItemDto(assessment.Year, assessment.PolicyCode, assessment.ComputedAt, TaxYearSummaryDto.From(assessment.Summary)));

        return new TaxOverviewDto([.. years], TaxAccountMapping.AccountsOf(refreshed.Taxpayer));
    }
}
