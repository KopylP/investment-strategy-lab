namespace InvestmentLab.Notifications.Delivery.Domain;

/// <summary>Something worth telling the user, whatever the channel it goes through.</summary>
/// <param name="Topic">What happened, e.g. <c>report_ready</c>: a channel may route on it.</param>
/// <param name="Tag">What it is about: a later notification with the same tag replaces it, a dismissal of the tag removes it.</param>
/// <param name="Data">The facts behind the text, by name, for channels that hand them to automations.</param>
public sealed record Notification(string Topic, string Tag, string Title, string Message, IReadOnlyDictionary<string, object?> Data);

/// <summary>
/// A way to reach the user: Home Assistant when the app runs as its add-on; Telegram or e-mail can follow. A module
/// registers a channel only when it is configured, so without any the app works as before and nothing is sent.
/// </summary>
public interface INotificationChannel
{
    string Name { get; }

    Task SendAsync(Notification notification, CancellationToken cancellationToken);

    /// <summary>Takes back what was sent under the tag, where the channel can (it is no longer news).</summary>
    Task DismissAsync(string tag, CancellationToken cancellationToken);
}
