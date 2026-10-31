using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Security;

public interface ICurrentUser
{
    UserId? Id { get; }

    UserId RequiredId => Id ?? throw new InvalidOperationException("The request has no signed-in user.");
}
