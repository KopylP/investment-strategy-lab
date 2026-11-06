using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.IntegrationTests.Identity;
using ModelContextProtocol.Client;

namespace InvestmentLab.IntegrationTests.Mcp;

public sealed class McpAccessTests(McpApiFactory factory) : IClassFixture<McpApiFactory>
{
    private const string McpPath = "/api/investlab/mcp";

    private static readonly string[] ResearchLoopTools =
        ["create_signal", "import_rules", "replace_draft_rules", "check_draft", "run_trial", "list_trials", "get_backtest", "get_experiment_journal"];

    [Fact]
    public async Task Without_a_token_the_client_is_told_where_to_get_one()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();

        var refused = await client.PostAsJsonAsync(McpPath, new { }, ct);

        refused.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var challenge = refused.Headers.WwwAuthenticate.ToString();
        challenge.ShouldContain("resource_metadata=");

        var metadataPath = new Uri(ResourceMetadataOf(challenge)).AbsolutePath;
        var metadata = await client.GetFromJsonAsync<JsonElement>(metadataPath, ct);
        metadata.GetProperty("resource").GetString().ShouldEndWith(McpPath);
        metadata.GetProperty("authorization_servers")[0].GetString().ShouldBe("http://localhost");
    }

    [Fact]
    public async Task A_token_its_issuer_refuses_is_not_let_in()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = WithToken(factory.CreateClient(), FakeHomeAssistantTokens.RejectedUser);

        var refused = await client.PostAsJsonAsync(McpPath, new { }, ct);

        refused.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_home_assistant_user_who_never_opened_the_panel_is_asked_to_do_it_once()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = WithToken(factory.CreateClient(), NewHomeAssistantUser());

        var refused = await client.PostAsJsonAsync(McpPath, new { }, ct);

        refused.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await refused.Content.ReadAsStringAsync(ct)).ShouldContain("identity.access_token.not_linked");
    }

    [Fact]
    public async Task A_linked_home_assistant_user_works_through_mcp_as_themselves()
    {
        var ct = TestContext.Current.CancellationToken;
        var homeAssistantUser = await LinkedHomeAssistantUserAsync(ct);

        await using var mcp = await ConnectAsync(homeAssistantUser, ct);

        var tools = await mcp.ListToolsAsync(cancellationToken: ct);
        ResearchLoopTools.ShouldBeSubsetOf(tools.Select(tool => tool.Name));

        var accounts = await mcp.CallToolAsync("list_accounts", cancellationToken: ct);
        accounts.IsError.ShouldNotBe(true);

        var vocabulary = await mcp.CallToolAsync("get_signal_vocabulary", cancellationToken: ct);
        TextOf(vocabulary).ShouldContain("InstrumentGroup");
    }

    [Fact]
    public async Task A_failed_tool_call_explains_itself_in_the_users_language()
    {
        var ct = TestContext.Current.CancellationToken;
        var homeAssistantUser = await LinkedHomeAssistantUserAsync(ct);
        await using var mcp = await ConnectAsync(homeAssistantUser, ct);

        var arguments = new Dictionary<string, object?> { ["draftId"] = Guid.NewGuid() };
        var missing = await mcp.CallToolAsync("get_draft", arguments, cancellationToken: ct);

        missing.IsError.ShouldBe(true);
        TextOf(missing).ShouldContain("[builder.");
    }

    private async Task<string> LinkedHomeAssistantUserAsync(CancellationToken ct)
    {
        var homeAssistantUser = NewHomeAssistantUser();
        var email = $"investor-{Guid.NewGuid():N}@example.com";
        var credentials = new { email, password = SignedInClient.Password };
        var panel = factory.CreateClient();
        panel.DefaultRequestHeaders.Add(HomeAssistantApiFactory.PeerHeader, HomeAssistantApiFactory.Supervisor);

        (await panel.PostAsJsonAsync("/api/identity/register", credentials, ct)).EnsureSuccessStatusCode();
        (await panel.PostAsJsonAsync("/api/identity/login", credentials, ct)).EnsureSuccessStatusCode();
        panel.DefaultRequestHeaders.Add("X-Remote-User-Id", homeAssistantUser);
        (await panel.GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        return homeAssistantUser;
    }

    private async Task<McpClient> ConnectAsync(string homeAssistantUser, CancellationToken ct)
    {
        var http = factory.CreateClient();
        var options = new HttpClientTransportOptions
        {
            Endpoint = new Uri(http.BaseAddress!, McpPath),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {FakeHomeAssistantTokens.TokenFor(homeAssistantUser)}",
            },
        };
        var transport = new HttpClientTransport(options, http, ownsHttpClient: true);

        return await McpClient.CreateAsync(transport, cancellationToken: ct);
    }

    private static HttpClient WithToken(HttpClient client, string homeAssistantUser)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeHomeAssistantTokens.TokenFor(homeAssistantUser));

        return client;
    }

    private static string ResourceMetadataOf(string challenge)
    {
        const string marker = "resource_metadata=\"";
        var start = challenge.IndexOf(marker, StringComparison.Ordinal) + marker.Length;

        return challenge[start..challenge.IndexOf('"', start)];
    }

    private static string TextOf(ModelContextProtocol.Protocol.CallToolResult result) =>
        string.Concat(result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(block => block.Text));

    private static string NewHomeAssistantUser() => Guid.NewGuid().ToString("N");
}
