using InvestmentLab.SharedKernel;
using InvestmentLab.Notifications.Delivery.Application.GetNotificationSettings;
using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Application.ChangeNotificationSettings;

public sealed record ChangeNotificationSettingsCommand(IReadOnlyList<string>? Destinations);

public sealed class ChangeNotificationSettingsHandler(
    INotificationPreferencesRepository preferences, GetNotificationSettingsHandler settings, TimeProvider clock) : IHandler
{
    public async Task<Result<NotificationSettingsDto>> Handle(
        UserId userId,
        ChangeNotificationSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var current = await FindOrAddAsync(userId, cancellationToken);

        current.Choose(command.Destinations, clock.GetUtcNow());
        await preferences.SaveChangesAsync(cancellationToken);

        return await settings.Handle(userId, cancellationToken);
    }

    private async Task<NotificationPreferences> FindOrAddAsync(UserId userId, CancellationToken cancellationToken)
    {
        var existing = await preferences.FindAsync(userId, cancellationToken);
        if (existing is not null)
            return existing;

        var created = NotificationPreferences.For(userId);
        preferences.Add(created);

        return created;
    }
}
