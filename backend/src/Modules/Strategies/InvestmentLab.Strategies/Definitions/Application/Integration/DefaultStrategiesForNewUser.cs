using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Identity.Contracts;
using InvestmentLab.Strategies.Definitions.Application.CreateDefaultStrategies;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Strategies.Definitions.Application.Integration;

internal sealed class DefaultStrategiesForNewUser(CreateDefaultStrategiesHandler create, ILogger<DefaultStrategiesForNewUser> logger)
    : IIntegrationEventHandler<UserRegisteredV1>
{
    public async Task Handle(UserRegisteredV1 registered, CancellationToken cancellationToken)
    {
        var result = await create.Handle(UserId.Of(registered.UserId), cancellationToken);
        if (result.IsFailure)
            logger.LogWarning("Default strategies for user {UserId} were not created: {Error}", registered.UserId, result.Error.Message);
    }
}
