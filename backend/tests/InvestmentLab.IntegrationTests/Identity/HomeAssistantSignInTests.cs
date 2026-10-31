using System.Net;
using System.Net.Http.Json;

namespace InvestmentLab.IntegrationTests.Identity;

public sealed class HomeAssistantSignInTests(HomeAssistantApiFactory factory) : IClassFixture<HomeAssistantApiFactory>
{
    private sealed record Me(Guid Id, string Email, bool ViaHomeAssistant);

    [Fact]
    public async Task A_home_assistant_user_without_an_account_is_asked_to_sign_in()
    {
        var panel = Panel(NewHomeAssistantUser());

        (await panel.GetAsync("/api/identity/me", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_account_signed_in_in_the_panel_becomes_the_home_assistant_users_and_then_needs_no_password()
    {
        var ct = TestContext.Current.CancellationToken;
        var homeAssistantUser = NewHomeAssistantUser();
        var email = $"investor-{Guid.NewGuid():N}@example.com";

        var signedIn = await factory.CreateSignedInClientAsync(ct, email);
        FromPanel(signedIn, homeAssistantUser);
        var linked = await signedIn.GetFromJsonAsync<Me>("/api/identity/me", ct);
        linked!.Email.ShouldBe(email);
        linked.ViaHomeAssistant.ShouldBeTrue();

        var me = await Panel(homeAssistantUser).GetFromJsonAsync<Me>("/api/identity/me", ct);
        me.ShouldBe(linked);
        (await Panel(homeAssistantUser).GetAsync("/api/portfolio/accounts", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await signedIn.GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await factory.CreateClient().GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("172.30.32.2:8099")]
    [InlineData("172.30.32.5:8098")]
    public async Task Home_assistant_headers_count_only_from_the_supervisor_on_the_ingress_port(string peer)
    {
        var ct = TestContext.Current.CancellationToken;
        var homeAssistantUser = NewHomeAssistantUser();
        var signedIn = await factory.CreateSignedInClientAsync(ct);
        FromPanel(signedIn, homeAssistantUser);
        (await signedIn.GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var impostor = factory.CreateClient();
        impostor.DefaultRequestHeaders.Add(HomeAssistantApiFactory.PeerHeader, peer);
        impostor.DefaultRequestHeaders.Add("X-Remote-User-Id", homeAssistantUser);

        (await impostor.GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Each_home_assistant_user_has_their_own_account()
    {
        var ct = TestContext.Current.CancellationToken;
        var first = NewHomeAssistantUser();
        var signedIn = await factory.CreateSignedInClientAsync(ct);
        FromPanel(signedIn, first);
        await signedIn.GetAsync("/api/identity/me", ct);

        (await Panel(NewHomeAssistantUser()).GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static string NewHomeAssistantUser() => Guid.NewGuid().ToString("N");

    private HttpClient Panel(string homeAssistantUser)
    {
        var client = factory.CreateClient();
        FromPanel(client, homeAssistantUser);

        return client;
    }

    private static void FromPanel(HttpClient client, string homeAssistantUser)
    {
        client.DefaultRequestHeaders.Add(HomeAssistantApiFactory.PeerHeader, HomeAssistantApiFactory.Supervisor);
        client.DefaultRequestHeaders.Add("X-Remote-User-Id", homeAssistantUser);
        client.DefaultRequestHeaders.Add("X-Remote-User-Name", "pavlo");
    }
}
