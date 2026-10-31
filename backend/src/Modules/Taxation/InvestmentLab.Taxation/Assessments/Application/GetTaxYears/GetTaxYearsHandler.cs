using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxYears;

public sealed class GetTaxYearsHandler(TaxRecalculator recalculator, IAssessmentRepository assessments, ICurrentUser currentUser)
{
    public async Task<TaxOverviewDto> Handle(CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        var outcome = await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        var assessed = await assessments.ForOwnerAsync(owner, cancellationToken);
        var years = assessed
            .OrderByDescending(assessment => assessment.Year)
            .Select(assessment => new TaxYearListItemDto(assessment.Year, assessment.PolicyCode, assessment.ComputedAt, assessment.Summary));

        return new TaxOverviewDto([.. years], TaxAccountMapping.AccountsOf(outcome.Taxpayer));
    }
}
