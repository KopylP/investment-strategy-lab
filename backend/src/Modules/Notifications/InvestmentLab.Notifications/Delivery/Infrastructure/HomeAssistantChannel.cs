using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Notifications.Delivery.Infrastructure;

internal sealed class HomeAssistantChannel(HttpClient http, HomeAssistantChannelOptions options, IStrategyDirectory strategies) : INotificationChannel
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
        var language = recipient.ChosenLanguage ?? await HomeAssistantLanguageAsync(cancellationToken);
        var localizer = new MessageLocalizer(language, await StrategyMessagesAsync(language, cancellationToken));
        var title = localizer.Render(notification.Title);
        var message = localizer.Render(notification.Message);

        await PostAsync($"core/api/events/investlab_{notification.Topic}",
            new Dictionary<string, object?>(notification.Data)
            {
                ["user_id"] = recipient.HomeAssistantUserId,
                ["tag"] = notification.Tag,
                ["title"] = title,
                ["message"] = message,
            },
            cancellationToken);

        var link = options.PanelPath is { Length: > 0 } path ? $"\n\n[{localizer.Render("notification.open_panel", null)}]({path})" : "";
        await PostAsync("core/api/services/persistent_notification/create",
            new { title, message = message + link, notification_id = notification.Tag }, cancellationToken);

        foreach (var phone in recipient.Targets(await DestinationsAsync(recipient, cancellationToken)))
        {
            await PostAsync($"core/api/services/notify/{phone.Id[Key.Length..]}", new
            {
                title,
                message,
                data = new { tag = notification.Tag, group = "investlab", url = options.PanelPath, clickAction = options.PanelPath },
            }, cancellationToken);
        }
    }

    public async Task DismissAsync(string tag, CancellationToken cancellationToken)
    {
        await PostAsync("core/api/services/persistent_notification/dismiss", new { notification_id = tag }, cancellationToken);

        foreach (var phone in await PhonesAsync(cancellationToken))
            await PostAsync($"core/api/services/notify/{phone}", new { message = "clear_notification", data = new { tag } }, cancellationToken);
    }

    private async Task<Language> HomeAssistantLanguageAsync(CancellationToken cancellationToken)
    {
        var config = await http.GetFromJsonAsync<HomeAssistantConfig>("core/api/config", cancellationToken);

        return Language.OfCultureOrDefault(config?.Language ?? "");
    }

    private async Task<IReadOnlyDictionary<string, string>?> StrategyMessagesAsync(Language language, CancellationToken cancellationToken)
    {
        try
        {
            return await strategies.GetMessages(language.Code, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

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

    private sealed record HomeAssistantConfig(string? Language);

    private sealed record ServiceDomain(string Domain, Dictionary<string, JsonElement> Services);

    private sealed record EntityState(
        [property: System.Text.Json.Serialization.JsonPropertyName("entity_id")] string EntityId,
        Dictionary<string, JsonElement>? Attributes)
    {
        public JsonElement? Attribute(string name) => Attributes?.TryGetValue(name, out var value) == true ? value : null;
    }
}
