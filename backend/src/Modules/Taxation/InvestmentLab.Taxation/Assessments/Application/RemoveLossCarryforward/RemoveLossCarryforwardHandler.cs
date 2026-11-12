using InvestmentLab.SharedKernel;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.RemoveLossCarryforward;

public sealed class RemoveLossCarryforwardHandler(TaxRecalculator recalculator, IAssessmentRepository assessments, ICurrentUser currentUser) : IHandler
{
    public async Task<Result> Handle(Guid lossId, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;

        var losses = await assessments.LossesAsync(owner, cancellationToken);
        if (losses.FirstOrDefault(loss => loss.Id == lossId) is not { } loss)
            return Error.NotFound("tax.loss_not_found", $"Loss {lossId} does not exist.");

        assessments.RemoveLoss(loss);
        await assessments.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        return Result.Success();
    }
}
