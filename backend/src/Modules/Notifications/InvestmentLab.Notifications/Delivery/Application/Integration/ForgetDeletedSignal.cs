using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class ForgetDeletedSignal(INotificationPreferencesRepository preferences) : IIntegrationEventHandler<SignalDeletedV1>
{
    public async Task Handle(SignalDeletedV1 deleted, CancellationToken cancellationToken)
    {
        var owner = UserId.Of(deleted.OwnerId);
        var ownerPreferences = await preferences.FindAsync(owner, cancellationToken);
        if (ownerPreferences is null || ownerPreferences.Hears(deleted.SignalId))
            return;

        ownerPreferences.HearSignal(deleted.SignalId, notify: true, deleted.DeletedAt);

        await preferences.SaveChangesAsync(cancellationToken);
    }
}
