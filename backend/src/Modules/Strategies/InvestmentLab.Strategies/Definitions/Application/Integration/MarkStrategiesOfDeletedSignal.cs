using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.Integration;

internal sealed class MarkStrategiesOfDeletedSignal(IStrategyRepository strategies) : IIntegrationEventHandler<SignalDeletedV1>
{
    public async Task Handle(SignalDeletedV1 message, CancellationToken cancellationToken)
    {
        var all = await strategies.AllAsync(cancellationToken);

        foreach (var strategy in all.Where(strategy => strategy.OwnerId.Value == message.OwnerId))
            strategy.MarkSignalDeleted(message.SignalId);

        await strategies.SaveChangesAsync(cancellationToken);
    }
}
