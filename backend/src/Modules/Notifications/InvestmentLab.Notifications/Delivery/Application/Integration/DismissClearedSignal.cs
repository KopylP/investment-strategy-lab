using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Application.Shared;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class DismissClearedSignal(Notifier notifier) : IIntegrationEventHandler<SignalClearedV1>
{
    public async Task Handle(SignalClearedV1 cleared, CancellationToken cancellationToken)
    {
        if (cleared.StillActive > 0)
            return;

        await notifier.DismissAsync(SignalNotification.TagOf(cleared.SignalId), cancellationToken);
    }
}
