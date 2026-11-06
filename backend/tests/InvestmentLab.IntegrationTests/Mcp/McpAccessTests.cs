using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.IntegrationTests.Identity;
using InvestmentLab.Strategies.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace InvestmentLab.IntegrationTests.Mcp;

public sealed class McpAccessTests : IClassFixture<McpApiFactory>
{
    private const string McpPath = "/api/investlab/mcp";

    private static readonly string[] ResearchLoopTools =
        ["create_signal", "import_rules", "replace_draft_rules", "check_draft", "run_trial", "list_trials", "get_backtest", "get_experiment_journal"];

    private readonly McpApiFactory _factory;

    public McpAccessTests(AppHostFixture appHost, McpApiFactory factory)
    {
        _factory = factory;
        _factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Fact]
    public async Task Without_a_token_the_client_is_told_where_to_get_one()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = _factory.CreateClient();

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
        var client = WithToken(_factory.CreateClient(), FakeHomeAssistantTokens.RejectedUser);

        var refused = await client.PostAsJsonAsync(McpPath, new { }, ct);

        refused.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_home_assistant_user_who_never_opened_the_panel_is_asked_to_do_it_once()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = WithToken(_factory.CreateClient(), NewHomeAssistantUser());

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
    public async Task The_blocks_of_strategy_rules_are_offered_even_when_the_stored_catalog_lacks_their_type()
    {
        var ct = TestContext.Current.CancellationToken;
        var homeAssistantUser = await LinkedHomeAssistantUserAsync(ct);
        await using var mcp = await ConnectAsync(homeAssistantUser, ct);
        (await mcp.CallToolAsync("get_vocabulary", cancellationToken: ct)).IsError.ShouldNotBe(true);

        await ForgetStoredStrategyTypeAsync("rule-based", ct);
        var vocabulary = await mcp.CallToolAsync("get_vocabulary", cancellationToken: ct);

        vocabulary.IsError.ShouldNotBe(true, TextOf(vocabulary));
        TextOf(vocabulary).ShouldContain("momentum");
    }

    [Fact]
    public async Task Texts_of_the_strategy_catalog_reach_the_assistant_in_ukrainian()
    {
        var ct = TestContext.Current.CancellationToken;
        var homeAssistantUser = await LinkedHomeAssistantUserAsync(ct);
        await using var mcp = await ConnectAsync(homeAssistantUser, ct);

        var vocabulary = TextOf(await mcp.CallToolAsync("get_vocabulary", cancellationToken: ct));

        vocabulary.ShouldNotContain("@rule_based.");
        vocabulary.ShouldContain("Що робити з триманими фондами");
    }

    [Fact]
    public async Task An_assistant_imports_rules_written_as_a_json_object()
    {
        var ct = TestContext.Current.CancellationToken;
        var homeAssistantUser = await LinkedHomeAssistantUserAsync(ct);
        await using var mcp = await ConnectAsync(homeAssistantUser, ct);
        var arguments = new Dictionary<string, object?> { ["name"] = "Imported", ["rules"] = HoldEverythingRules() };

        var imported = await mcp.CallToolAsync("import_rules", arguments, cancellationToken: ct);

        imported.IsError.ShouldNotBe(true, TextOf(imported));
        TextOf(imported).ShouldContain("Imported");
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
        var panel = _factory.CreateClient();
        panel.DefaultRequestHeaders.Add(HomeAssistantApiFactory.PeerHeader, HomeAssistantApiFactory.Supervisor);

        (await panel.PostAsJsonAsync("/api/identity/register", credentials, ct)).EnsureSuccessStatusCode();
        (await panel.PostAsJsonAsync("/api/identity/login", credentials, ct)).EnsureSuccessStatusCode();
        panel.DefaultRequestHeaders.Add("X-Remote-User-Id", homeAssistantUser);
        (await panel.GetAsync("/api/identity/me", ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        return homeAssistantUser;
    }

    private static JsonElement HoldEverythingRules() =>
        JsonSerializer.SerializeToElement(new
        {
            formatVersion = 1,
            indicators = new { },
            selection = new { top = 3 },
            weights = new { method = "equal" },
        });

    private async Task ForgetStoredStrategyTypeAsync(string code, CancellationToken ct)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StrategiesDbContext>();

        await db.StrategyTypes.Where(type => type.Code == code).ExecuteDeleteAsync(ct);
    }

    private async Task<McpClient> ConnectAsync(string homeAssistantUser, CancellationToken ct)
    {
        var http = _factory.CreateClient();
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
