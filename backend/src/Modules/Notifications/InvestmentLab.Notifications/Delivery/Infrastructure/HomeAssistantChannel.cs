using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Infrastructure;

/// <param name="PanelPath">The add-on's sidebar panel in Home Assistant's frontend, e.g. <c>/hassio/ingress/local_investlab</c>; null: no link.</param>
public sealed record HomeAssistantChannelOptions(string? PanelPath)
{
    public const string Section = "Notifications:HomeAssistant";
}

/// <summary>
/// Home Assistant, through the Supervisor's proxy to its Core API (the add-on's <c>homeassistant_api</c> and its
/// <c>SUPERVISOR_TOKEN</c>): fires <c>investlab_&lt;topic&gt;</c> on its event bus for automations, shows a persistent
/// notification in its sidebar and pushes to the recipient's phones, all under the notification's tag, so a dismissal
/// clears them again.
/// </summary>
/// <remarks>
/// The phones are the Companion app's <c>notify.mobile_app_&lt;device&gt;</c> services. The app also creates
/// <c>device_tracker.&lt;device&gt;</c>, which Home Assistant's person of that user lists: so a user's own phones are
/// suggested with nothing to set up, as soon as they opened the panel once (that links their Home Assistant user).
/// </remarks>
internal sealed class HomeAssistantChannel(HttpClient http, HomeAssistantChannelOptions options) : INotificationChannel
{
    public const string SupervisorAddress = "http://supervisor/";
    private const string Key = "homeassistant:";
    private const string MobileApp = "mobile_app_";

    public string Name => "Home Assistant";

    public async Task<IReadOnlyList<Destination>> DestinationsAsync(Recipient recipient, CancellationToken cancellationToken)
    {
        var phones = await PhonesAsync(cancellationToken);
        var states = await http.GetFromJsonAsync<List<EntityState>>("core/api/states", cancellationToken) ?? [];

        var own = states
            .Where(state => state.EntityId.StartsWith("person.", StringComparison.Ordinal)
                            && recipient.HomeAssistantUserId is not null
                            && state.Attribute("user_id")?.GetString() == recipient.HomeAssistantUserId)
            .SelectMany(person => person.Attribute("device_trackers") is { ValueKind: JsonValueKind.Array } trackers
                ? trackers.EnumerateArray().Select(tracker => tracker.GetString() ?? "")
                : [])
            .ToHashSet();
        var names = states
            .Where(state => state.EntityId.StartsWith("device_tracker.", StringComparison.Ordinal))
            .ToDictionary(state => state.EntityId, state => state.Attribute("friendly_name")?.GetString());

        return [.. phones.Select(service =>
        {
            var tracker = $"device_tracker.{service[MobileApp.Length..]}";
            return new Destination(Key + service, names.GetValueOrDefault(tracker) ?? service, own.Contains(tracker));
        })];
    }

    public async Task SendAsync(Notification notification, Recipient recipient, CancellationToken cancellationToken)
    {
        await PostAsync($"core/api/events/investlab_{notification.Topic}",
            new Dictionary<string, object?>(notification.Data)
            {
                ["user_id"] = recipient.HomeAssistantUserId,
                ["tag"] = notification.Tag,
                ["title"] = notification.Title,
                ["message"] = notification.Message,
            },
            cancellationToken);

        var link = options.PanelPath is { Length: > 0 } path ? $"\n\n[Відкрити Investment Lab]({path})" : "";
        await PostAsync("core/api/services/persistent_notification/create",
            new { title = notification.Title, message = notification.Message + link, notification_id = notification.Tag }, cancellationToken);

        foreach (var phone in recipient.Targets(await DestinationsAsync(recipient, cancellationToken)))
        {
            // url opens the panel on iOS, clickAction on Android.
            await PostAsync($"core/api/services/notify/{phone.Id[Key.Length..]}", new
            {
                title = notification.Title,
                message = notification.Message,
                data = new { tag = notification.Tag, group = "investlab", url = options.PanelPath, clickAction = options.PanelPath },
            }, cancellationToken);
        }
    }

    /// <summary>Every phone, not only the owner's: who got it may have changed since.</summary>
    public async Task DismissAsync(string tag, CancellationToken cancellationToken)
    {
        await PostAsync("core/api/services/persistent_notification/dismiss", new { notification_id = tag }, cancellationToken);
        foreach (var phone in await PhonesAsync(cancellationToken))
            await PostAsync($"core/api/services/notify/{phone}", new { message = "clear_notification", data = new { tag } }, cancellationToken);
    }

    /// <summary>The Companion app's notify services, e.g. <c>mobile_app_iphone</c>.</summary>
    private async Task<IReadOnlyList<string>> PhonesAsync(CancellationToken cancellationToken)
    {
        var domains = await http.GetFromJsonAsync<List<ServiceDomain>>("core/api/services", cancellationToken) ?? [];
        return [.. domains.Where(domain => domain.Domain == "notify")
            .SelectMany(domain => domain.Services.Keys)
            .Where(service => service.StartsWith(MobileApp, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)];
    }

    private async Task PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(path, body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ServiceDomain(string Domain, Dictionary<string, JsonElement> Services);

    private sealed record EntityState(
        [property: System.Text.Json.Serialization.JsonPropertyName("entity_id")] string EntityId,
        Dictionary<string, JsonElement>? Attributes)
    {
        public JsonElement? Attribute(string name) => Attributes?.TryGetValue(name, out var value) == true ? value : null;
    }
}
