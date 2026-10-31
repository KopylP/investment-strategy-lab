using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class AnnounceFiredSignal(Notifier notifier, INotificationPreferencesRepository preferences) : IIntegrationEventHandler<SignalTriggeredV1>
{
    public async Task Handle(SignalTriggeredV1 triggered, CancellationToken cancellationToken)
    {
        var owner = UserId.Of(triggered.OwnerId);
        var ownerPreferences = await preferences.FindAsync(owner, cancellationToken);

        var isMuted = ownerPreferences is not null && !ownerPreferences.Hears(triggered.SignalId);
        if (isMuted)
            return;

        await notifier.SendAsync(SignalNotification.For(triggered), cancellationToken);
    }
}
