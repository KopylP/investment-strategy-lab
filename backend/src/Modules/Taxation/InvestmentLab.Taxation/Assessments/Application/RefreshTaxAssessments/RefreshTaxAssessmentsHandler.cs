using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;

namespace InvestmentLab.Taxation.Assessments.Application.RefreshTaxAssessments;

public sealed class RefreshTaxAssessmentsHandler(TaxRecalculator recalculator, ICurrentUser currentUser)
{
    public Task<RecomputeOutcome> Handle(CancellationToken cancellationToken) =>
        recalculator.RecomputeOwnerAsync(currentUser.RequiredId, cancellationToken);
}
