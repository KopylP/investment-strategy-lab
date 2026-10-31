using InvestmentLab.Identity.Contracts;
using InvestmentLab.Notifications.Delivery.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Notifications.Delivery.Application;

public sealed class Notifier(
    IEnumerable<INotificationChannel> channels, INotificationPreferencesRepository preferences, IHomeAssistantUsers homeAssistantUsers, ILogger<Notifier> logger)
{
    public IReadOnlyList<INotificationChannel> Channels { get; } = [.. channels];

    public async Task<Recipient> RecipientAsync(Guid userId, CancellationToken cancellationToken) => new(
        userId,
        await homeAssistantUsers.FindHomeAssistantUserIdAsync(userId, cancellationToken),
        (await preferences.FindAsync(UserId.Of(userId), cancellationToken))?.Destinations);

    public async Task<IReadOnlyList<string>> SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        if (Channels.Count == 0)
            return [];

        var recipient = await RecipientAsync(notification.RecipientId, cancellationToken);

        return await EachAsync(channel => channel.SendAsync(notification, recipient, cancellationToken), "send", notification.Tag);
    }

    public Task<IReadOnlyList<string>> DismissAsync(string tag, CancellationToken cancellationToken) =>
        EachAsync(channel => channel.DismissAsync(tag, cancellationToken), "dismiss", tag);

    private async Task<IReadOnlyList<string>> EachAsync(Func<INotificationChannel, Task> action, string verb, string tag)
    {
        var failures = new List<string>();

        foreach (var channel in Channels)
        {
            try
            {
                await action(channel);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "{Channel} could not {Verb} notification {Tag}", channel.Name, verb, tag);
                failures.Add($"{channel.Name}: {exception.Message}");
            }
        }

        return failures;
    }
}
