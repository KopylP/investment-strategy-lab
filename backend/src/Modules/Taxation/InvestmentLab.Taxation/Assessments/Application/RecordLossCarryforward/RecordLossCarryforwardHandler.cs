using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.RecordLossCarryforward;

public sealed class RecordLossCarryforwardHandler(TaxRecalculator recalculator, IAssessmentRepository assessments, ICurrentUser currentUser)
{
    public async Task<Result<LossCarryforwardDto>> Handle(RecordLossCommand command, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;

        var loss = LossCarryforward.Record(owner, command.Year, command.Amount, command.AlreadyUsed);
        if (loss.IsFailure)
            return loss.Error;

        assessments.AddLoss(loss.Value);
        await assessments.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        return new LossCarryforwardDto(loss.Value.Id, loss.Value.Year, loss.Value.Amount, loss.Value.AlreadyUsed);
    }
}
