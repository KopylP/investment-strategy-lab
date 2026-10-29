namespace InvestmentLab.Notifications.Delivery.Domain;

/// <summary>Something worth telling a user, whatever the channel it goes through.</summary>
/// <param name="RecipientId">The user it is for: pushes go to their devices only.</param>
/// <param name="Topic">What happened, e.g. <c>report_ready</c>: a channel may route on it.</param>
/// <param name="Tag">What it is about: a later notification with the same tag replaces it, a dismissal of the tag removes it.</param>
/// <param name="Data">The facts behind the text, by name, for channels that hand them to automations.</param>
public sealed record Notification(Guid RecipientId, string Topic, string Tag, string Title, string Message, IReadOnlyDictionary<string, object?> Data);

/// <summary>The user a notification is for, with what channels need to find their devices.</summary>
/// <param name="HomeAssistantUserId">The Home Assistant user they opened the panel as; null if never.</param>
/// <param name="ChosenDestinations">The destinations the user picked; null: each channel's suggestion for them.</param>
public sealed record Recipient(Guid UserId, string? HomeAssistantUserId, IReadOnlyList<string>? ChosenDestinations)
{
    public IEnumerable<Destination> Targets(IEnumerable<Destination> available) =>
        ChosenDestinations is { } chosen ? available.Where(destination => chosen.Contains(destination.Id)) : available.Where(destination => destination.Suggested);
}

/// <summary>A device or address a channel can push to.</summary>
/// <param name="Id">Unique across channels: <c>&lt;channel&gt;:&lt;address&gt;</c>, e.g. <c>homeassistant:mobile_app_iphone</c>.</param>
/// <param name="Suggested">The channel believes it belongs to the recipient: used until they pick their own.</param>
public sealed record Destination(string Id, string Label, bool Suggested);

/// <summary>
/// A way to reach the user: Home Assistant when the app runs as its add-on; Telegram or e-mail can follow. A module
/// registers a channel only when it is configured, so without any the app works as before and nothing is sent.
/// </summary>
public interface INotificationChannel
{
    string Name { get; }

    /// <summary>Where this channel can push, the recipient's own marked <see cref="Destination.Suggested"/>.</summary>
    Task<IReadOnlyList<Destination>> DestinationsAsync(Recipient recipient, CancellationToken cancellationToken);

    Task SendAsync(Notification notification, Recipient recipient, CancellationToken cancellationToken);

    /// <summary>Takes back what was sent under the tag, wherever the channel can (it is no longer news).</summary>
    Task DismissAsync(string tag, CancellationToken cancellationToken);
}
