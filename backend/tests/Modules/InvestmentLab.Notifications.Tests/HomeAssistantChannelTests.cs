using System.Net;
using System.Text;
using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Notifications.Delivery.Infrastructure;

namespace InvestmentLab.Notifications.Tests;

public sealed class HomeAssistantChannelTests
{
    private const string Services = """
        [{"domain":"notify","services":{"mobile_app_iphone":{},"mobile_app_pixel":{},"persistent_notification":{}}},
         {"domain":"light","services":{"turn_on":{}}}]
        """;

    private const string States = """
        [{"entity_id":"person.petro","attributes":{"user_id":"ha-petro","device_trackers":["device_tracker.iphone"]}},
         {"entity_id":"person.olena","attributes":{"user_id":"ha-olena","device_trackers":["device_tracker.pixel"]}},
         {"entity_id":"device_tracker.iphone","attributes":{"friendly_name":"Petro's iPhone"}},
         {"entity_id":"device_tracker.pixel","attributes":{"friendly_name":"Pixel 8"}}]
        """;

    private readonly FakeHomeAssistant _homeAssistant = new();

    private HomeAssistantChannel Channel() =>
        new(new HttpClient(_homeAssistant) { BaseAddress = new Uri(HomeAssistantChannel.SupervisorAddress) }, new HomeAssistantChannelOptions("/hassio/ingress/local_investlab"));

    private static Notification Report(Guid recipient) =>
        new(recipient, "report_ready", "investlab_report_1", "Рапорт готовий", "3 дії", new Dictionary<string, object?>());

    [Fact]
    public async Task Every_Companion_app_phone_is_offered_and_the_users_own_through_their_person_is_suggested()
    {
        var destinations = await Channel().DestinationsAsync(new Recipient(Guid.NewGuid(), "ha-petro", null), CancellationToken.None);

        destinations.ShouldBe([
            new Destination("homeassistant:mobile_app_iphone", "Petro's iPhone", Suggested: true),
            new Destination("homeassistant:mobile_app_pixel", "Pixel 8", Suggested: false),
        ]);
    }

    [Fact]
    public async Task On_automatic_the_push_goes_only_to_the_recipients_own_phone()
    {
        await Channel().SendAsync(Report(Guid.NewGuid()), new Recipient(Guid.NewGuid(), "ha-olena", null), CancellationToken.None);

        _homeAssistant.Posts.ShouldBe([
            "core/api/events/investlab_report_ready",
            "core/api/services/persistent_notification/create",
            "core/api/services/notify/mobile_app_pixel",
        ]);
    }

    [Fact]
    public async Task A_user_who_picked_gets_pushes_on_the_phones_they_picked()
    {
        var picked = new Recipient(Guid.NewGuid(), "ha-petro", ["homeassistant:mobile_app_pixel", "homeassistant:mobile_app_iphone"]);

        await Channel().SendAsync(Report(Guid.NewGuid()), picked, CancellationToken.None);

        _homeAssistant.Posts.Where(post => post.Contains("notify/")).ShouldBe([
            "core/api/services/notify/mobile_app_iphone",
            "core/api/services/notify/mobile_app_pixel",
        ]);
    }

    [Fact]
    public async Task A_user_never_seen_through_the_panel_gets_no_push_until_they_pick()
    {
        await Channel().SendAsync(Report(Guid.NewGuid()), new Recipient(Guid.NewGuid(), null, null), CancellationToken.None);

        _homeAssistant.Posts.ShouldNotContain(post => post.Contains("notify/"));
    }

    private sealed class FakeHomeAssistant : HttpMessageHandler
    {
        public List<string> Posts { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            if (request.Method == HttpMethod.Post)
                Posts.Add(path);
            var body = path switch
            {
                "core/api/services" when request.Method == HttpMethod.Get => Services,
                "core/api/states" => States,
                _ => "[]",
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
