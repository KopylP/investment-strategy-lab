using InvestmentLab.SharedKernel;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;

namespace InvestmentLab.Taxation.Assessments.Application.RefreshTaxAssessments;

public sealed class RefreshTaxAssessmentsHandler(TaxRecalculator recalculator, ICurrentUser currentUser) : IHandler
{
    public async Task Handle(CancellationToken cancellationToken) =>
        await recalculator.RecomputeOwnerAsync(currentUser.RequiredId, cancellationToken);
}
