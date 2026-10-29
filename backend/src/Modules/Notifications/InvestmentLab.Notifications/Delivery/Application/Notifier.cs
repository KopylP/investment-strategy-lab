using InvestmentLab.Notifications.Delivery.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Notifications.Delivery.Application;

/// <summary>
/// Hands a notification to every configured channel. Best effort: a channel that fails is logged and skipped, never
/// thrown — the events it reacts to have other handlers, which an exception would make the outbox deliver again.
/// </summary>
public sealed class Notifier(IEnumerable<INotificationChannel> channels, ILogger<Notifier> logger)
{
    public Task SendAsync(Notification notification, CancellationToken cancellationToken) =>
        EachAsync(channel => channel.SendAsync(notification, cancellationToken), "send", notification.Tag);

    public Task DismissAsync(string tag, CancellationToken cancellationToken) =>
        EachAsync(channel => channel.DismissAsync(tag, cancellationToken), "dismiss", tag);

    private async Task EachAsync(Func<INotificationChannel, Task> action, string verb, string tag)
    {
        foreach (var channel in channels)
        {
            try
            {
                await action(channel);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "{Channel} could not {Verb} notification {Tag}", channel.Name, verb, tag);
            }
        }
    }
}
