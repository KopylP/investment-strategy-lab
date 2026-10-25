using System.Net;
using System.Text.Json;

namespace InvestmentLab.IntegrationTests;

public sealed class StrategyEngineTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Strategy_engine_is_started_by_the_app_host()
    {
        using var client = fixture.CreateStrategyEngineClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Strategy_engine_publishes_etf_momentum()
    {
        using var client = fixture.CreateStrategyEngineClient();

        var json = await client.GetStringAsync("/v1/strategies", TestContext.Current.CancellationToken);

        using var catalog = JsonDocument.Parse(json);
        catalog.RootElement.EnumerateArray().Select(item => item.GetProperty("code").GetString()).ShouldContain("etf-momentum");
    }
}
