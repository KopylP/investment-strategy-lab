using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Security;

/// <summary>The signed-in user of the current request; none in background jobs and outbox handlers.</summary>
public interface ICurrentUser
{
    UserId? Id { get; }

    UserId RequiredId => Id ?? throw new InvalidOperationException("The request has no signed-in user.");
}
