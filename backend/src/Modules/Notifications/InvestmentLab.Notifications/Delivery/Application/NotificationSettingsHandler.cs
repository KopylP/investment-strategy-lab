using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Application;

/// <param name="Selected">Gets the user's notifications: their choice, or the suggestion while they are on automatic.</param>
public sealed record DestinationDto(string Id, string Channel, string Label, bool Suggested, bool Selected);

/// <param name="Channels">The configured channels; empty outside Home Assistant (nothing is sent).</param>
/// <param name="Automatic">The user has not picked: each channel's suggestion is used.</param>
public sealed record NotificationSettingsDto(IReadOnlyList<string> Channels, bool Automatic, IReadOnlyList<DestinationDto> Destinations);

/// <param name="Destinations">Null: back to automatic.</param>
public sealed record ChangeNotificationSettingsCommand(IReadOnlyList<string>? Destinations);

/// <summary>The signed-in user's notification settings and a test notification to check them.</summary>
public sealed class NotificationSettingsHandler(Notifier notifier, INotificationPreferencesRepository preferences, TimeProvider clock)
{
    public async Task<Result<NotificationSettingsDto>> Get(UserId userId, CancellationToken cancellationToken)
    {
        var recipient = await notifier.RecipientAsync(userId.Value, cancellationToken);
        var destinations = new List<DestinationDto>();
        foreach (var channel in notifier.Channels)
        {
            IReadOnlyList<Destination> available;
            try
            {
                available = await channel.DestinationsAsync(recipient, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                return NotificationErrors.ChannelFailed(channel.Name, exception.Message);
            }

            var targets = recipient.Targets(available).Select(destination => destination.Id).ToHashSet();
            destinations.AddRange(available.Select(destination =>
                new DestinationDto(destination.Id, channel.Name, destination.Label, destination.Suggested, targets.Contains(destination.Id))));
        }

        return new NotificationSettingsDto([.. notifier.Channels.Select(channel => channel.Name)], recipient.ChosenDestinations is null, destinations);
    }

    public async Task<Result<NotificationSettingsDto>> Change(UserId userId, ChangeNotificationSettingsCommand command, CancellationToken cancellationToken)
    {
        var current = await preferences.FindAsync(userId, cancellationToken);
        if (current is null)
        {
            current = NotificationPreferences.For(userId);
            preferences.Add(current);
        }

        current.Choose(command.Destinations, clock.GetUtcNow());
        await preferences.SaveChangesAsync(cancellationToken);
        return await Get(userId, cancellationToken);
    }

    public async Task<Result> SendTest(UserId userId, CancellationToken cancellationToken)
    {
        if (notifier.Channels.Count == 0)
            return NotificationErrors.NoChannel();

        var failures = await notifier.SendAsync(new Notification(
            userId.Value, "test", $"investlab_test_{userId.Value:N}", "Investment Lab: тестове сповіщення",
            "Сповіщення працюють: сюди прийде звістка, коли буде готовий рапорт.", new Dictionary<string, object?>()), cancellationToken);
        return failures.Count == 0 ? Result.Success() : NotificationErrors.ChannelFailed(string.Join("; ", failures));
    }
}

public static class NotificationErrors
{
    public static Error NoChannel() =>
        Error.Conflict("notification.no_channel", "No notification channel is configured: the app does not run in Home Assistant.");

    public static Error ChannelFailed(string details) => Error.Conflict("notification.channel_failed", $"The notification was not delivered: {details}");

    public static Error ChannelFailed(string channel, string details) => ChannelFailed($"{channel}: {details}");
}
