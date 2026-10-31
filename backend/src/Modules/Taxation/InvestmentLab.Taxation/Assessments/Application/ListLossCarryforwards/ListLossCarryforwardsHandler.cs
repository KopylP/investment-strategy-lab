using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.ListLossCarryforwards;

public sealed class ListLossCarryforwardsHandler(IAssessmentRepository assessments, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<LossCarryforwardDto>> Handle(CancellationToken cancellationToken)
    {
        var losses = await assessments.LossesAsync(currentUser.RequiredId, cancellationToken);

        return [.. losses.OrderBy(loss => loss.Year).Select(loss => new LossCarryforwardDto(loss.Id, loss.Year, loss.Amount, loss.AlreadyUsed))];
    }
}
