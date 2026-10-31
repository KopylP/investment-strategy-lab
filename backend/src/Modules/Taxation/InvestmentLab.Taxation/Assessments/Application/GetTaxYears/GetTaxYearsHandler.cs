using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxYears;

public sealed class GetTaxYearsHandler(IAssessmentRepository assessments)
{
    public async Task<TaxOverviewDto> Handle(RecomputeOutcome refreshed, CancellationToken cancellationToken)
    {
        var assessed = await assessments.ForOwnerAsync(refreshed.Taxpayer.OwnerId, cancellationToken);

        var years = assessed
            .OrderByDescending(assessment => assessment.Year)
            .Select(assessment => new TaxYearListItemDto(assessment.Year, assessment.PolicyCode, assessment.ComputedAt, assessment.Summary));

        return new TaxOverviewDto([.. years], TaxAccountMapping.AccountsOf(refreshed.Taxpayer));
    }
}
