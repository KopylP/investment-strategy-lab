using System.Net.Http.Json;
using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Infrastructure;

/// <param name="NotifyServices">Home Assistant notify services that also get a push, e.g. <c>mobile_app_pixel</c> (with or without <c>notify.</c>).</param>
/// <param name="PanelPath">The add-on's sidebar panel in Home Assistant's frontend, e.g. <c>/hassio/ingress/local_investlab</c>; null: no link.</param>
public sealed record HomeAssistantChannelOptions(IReadOnlyList<string> NotifyServices, string? PanelPath)
{
    public const string Section = "Notifications:HomeAssistant";
}

/// <summary>
/// Home Assistant, through the Supervisor's proxy to its Core API (the add-on's <c>homeassistant_api</c> and its
/// <c>SUPERVISOR_TOKEN</c>): fires <c>investlab_&lt;topic&gt;</c> on its event bus for automations, shows a persistent
/// notification in its sidebar and pushes to the configured notify services (the Companion app's
/// <c>mobile_app_*</c>), all under the notification's tag, so a dismissal clears them again.
/// </summary>
internal sealed class HomeAssistantChannel(HttpClient http, HomeAssistantChannelOptions options) : INotificationChannel
{
    public const string SupervisorAddress = "http://supervisor/";

    public string Name => "Home Assistant";

    public async Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        await PostAsync($"core/api/events/investlab_{notification.Topic}",
            new Dictionary<string, object?>(notification.Data) { ["tag"] = notification.Tag, ["title"] = notification.Title, ["message"] = notification.Message },
            cancellationToken);

        var link = options.PanelPath is { Length: > 0 } path ? $"\n\n[Відкрити Investment Lab]({path})" : "";
        await PostAsync("core/api/services/persistent_notification/create",
            new { title = notification.Title, message = notification.Message + link, notification_id = notification.Tag }, cancellationToken);

        foreach (var service in options.NotifyServices)
        {
            // url opens the panel on iOS, clickAction on Android.
            await PostAsync($"core/api/services/notify/{ServiceName(service)}", new
            {
                title = notification.Title,
                message = notification.Message,
                data = new { tag = notification.Tag, group = "investlab", url = options.PanelPath, clickAction = options.PanelPath },
            }, cancellationToken);
        }
    }

    public async Task DismissAsync(string tag, CancellationToken cancellationToken)
    {
        await PostAsync("core/api/services/persistent_notification/dismiss", new { notification_id = tag }, cancellationToken);
        foreach (var service in options.NotifyServices)
            await PostAsync($"core/api/services/notify/{ServiceName(service)}", new { message = "clear_notification", data = new { tag } }, cancellationToken);
    }

    private async Task PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(path, body, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string ServiceName(string service) => service.Trim().StartsWith("notify.", StringComparison.Ordinal) ? service.Trim()["notify.".Length..] : service.Trim();
}
