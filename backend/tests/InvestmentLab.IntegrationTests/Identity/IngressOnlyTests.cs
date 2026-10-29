using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace InvestmentLab.IntegrationTests.Identity;

/// <summary>The add-on as deployed: only the Supervisor, on the ingress port, gets an answer.</summary>
public sealed class IngressOnlyApiFactory : HomeAssistantApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("HomeAssistant:IngressOnly", "true");
    }
}

public sealed class IngressOnlyTests(IngressOnlyApiFactory factory) : IClassFixture<IngressOnlyApiFactory>
{
    [Theory]
    [InlineData(null)]                // no Supervisor in front at all
    [InlineData("172.30.32.5:8098")]  // another add-on on the Supervisor's network
    [InlineData("172.30.32.2:8099")]  // not the ingress port
    public async Task Anyone_but_the_supervisor_gets_nothing_not_even_the_login(string? peer)
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        if (peer is not null)
            client.DefaultRequestHeaders.Add(HomeAssistantApiFactory.PeerHeader, peer);

        (await client.GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/api/identity/login", new { email = "a@example.com", password = "x" }, ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_supervisor_gets_the_app()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HomeAssistantApiFactory.PeerHeader, HomeAssistantApiFactory.Supervisor);

        (await client.GetAsync("/api/identity/me", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
