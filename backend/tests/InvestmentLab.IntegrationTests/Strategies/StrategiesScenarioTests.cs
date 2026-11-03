using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using InvestmentLab.IntegrationTests.Portfolio;
using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application.Shared;

namespace InvestmentLab.IntegrationTests.Strategies;

public sealed class StrategiesScenarioTests : IClassFixture<StrategiesApiFactory>
{
    private readonly AppHostFixture _appHost;
    private readonly StrategiesApiFactory _factory;

    public StrategiesScenarioTests(AppHostFixture appHost, StrategiesApiFactory factory)
    {
        (_appHost, _factory) = (appHost, factory);
        _factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Fact]
    public async Task Default_strategy_decides_on_the_imported_sub_portfolio_like_the_python_engine()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(ct);
        (await client.PostAsync("/api/market-data/quotes/ingest", null, ct)).EnsureSuccessStatusCode();

        var catalog = await client.GetFromJsonAsync<List<StrategyTypeDto>>("/api/strategies/catalog", ct);
        catalog!.ShouldContain(type => type.Code == "etf-momentum");

        var defaults = await client.DefaultStrategiesAsync<StrategyDto>(strategy => strategy.Name, ct);
        defaults.Select(strategy => strategy.Name).ShouldBe(["EU adaptive momentum", "EU index hold", "US adaptive momentum", "US index hold"]);
        defaults[0].Version.Universe.Count.ShouldBe(17);
        defaults[0].Version.Parameters.GetProperty("scanner").GetProperty("topN").GetInt32().ShouldBe(3);
        (await PostAsync<List<StrategyDto>>(client, "/api/strategies/defaults", new { }, ct)).ShouldBeEmpty();
        var eu = await client.CreateEtfMomentumOnAsync(defaults[0], ct);

        var account = await PortfolioScenarioTests.OpenTwoSleeveAccountAsync(client, ct);
        var sleeve = account.SubPortfolios[0].Id;
        var positions = await File.ReadAllTextAsync(PortfolioScenarioTests.PositionsJsonPath(), ct);
        (await client.PostAsync($"/api/portfolio/accounts/{account.Id}/sub-portfolios/{sleeve}/import-positions?asOf=2026-05-04",
            new StringContent(positions, System.Text.Encoding.UTF8, "application/json"), ct)).EnsureSuccessStatusCode();

        var asOf = SyntheticQuoteProvider.LastDay;
        var evaluate = new { accountId = account.Id, subPortfolioId = sleeve, asOf, contribution = 227m };
        _factory.EngineRequests.Clear();
        var decision = await PostAsync<StrategyDecisionDto>(client, $"/api/strategies/{eu.Id}/decide", evaluate, ct);

        decision.StrategyId.ShouldBe(eu.Id);
        decision.StrategyVersion.ShouldBe(1);
        decision.StrategyCode.ShouldBe("etf-momentum");
        decision.Market.IsFavorable.ShouldBeTrue(string.Join(" ", decision.Market.Reasons));
        decision.InvestFraction.ShouldBe(1m);
        decision.Targets.ShouldNotBeEmpty();
        decision.Targets.Count.ShouldBeLessThanOrEqualTo(4);
        decision.Targets.Select(target => target.InstrumentId).ShouldBeSubsetOf(eu.Version.Universe);

        var context = _factory.EngineRequests.ShouldHaveSingleItem();
        JsonNode.Parse(context)!["subPortfolio"]!["holdings"]!.AsArray().Count.ShouldBe(3);
        using var engine = _appHost.CreateStrategyEngineClient();
        var direct = await engine.PostAsync("/v1/strategies/etf-momentum/decide", new StringContent(context, System.Text.Encoding.UTF8, "application/json"), ct);
        using var answer = JsonDocument.Parse(await direct.Content.ReadAsStringAsync(ct));
        answer.RootElement.GetProperty("targets").EnumerateArray()
            .Select(target => (Guid.Parse(target.GetProperty("key").GetString()!), target.GetProperty("weight").GetDecimal()))
            .ShouldBe(decision.Targets.Select(target => (target.InstrumentId, target.Weight)));
        answer.RootElement.GetProperty("investFraction").GetDecimal().ShouldBe(decision.InvestFraction);

        var risk = await PostAsync<RiskAssessmentDto>(client, $"/api/strategies/{eu.Id}/risk", evaluate, ct);
        risk.Status.ShouldBeOneOf("ok", "watch", "act");
        risk.Positions.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Reconfiguring_creates_a_version_and_parameters_are_checked_against_the_schema()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(ct);
        var defaults = await client.DefaultStrategiesAsync<StrategyDto>(strategy => strategy.Name, ct);
        var eu = await client.CreateEtfMomentumOnAsync(defaults[0], ct);
        var setup = eu.Version;

        var invalid = await client.PutAsJsonAsync($"/api/strategies/{eu.Id}", new
        {
            universe = setup.Universe,
            benchmarkInstrumentId = setup.BenchmarkInstrumentId,
            parameters = new { scanner = new { topN = 0 } },
        }, ct);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await invalid.Content.ReadAsStringAsync(ct)).ShouldContain("strategy.parameters_invalid");

        var reconfigured = await client.PutAsJsonAsync($"/api/strategies/{eu.Id}", new
        {
            universe = setup.Universe.Take(5),
            benchmarkInstrumentId = setup.BenchmarkInstrumentId,
            volatilityIndexInstrumentId = setup.VolatilityIndexInstrumentId,
            cashInstrumentId = setup.CashInstrumentId,
            parameters = new { scanner = new { topN = 2, holdBufferK = 3 } },
        }, ct);
        reconfigured.StatusCode.ShouldBe(HttpStatusCode.OK, await reconfigured.Content.ReadAsStringAsync(ct));
        var latest = (await reconfigured.Content.ReadFromJsonAsync<StrategyDto>(ct))!;
        latest.LatestVersion.ShouldBe(2);
        latest.Version.Parameters.GetProperty("scanner").GetProperty("topN").GetInt32().ShouldBe(2);
        latest.Version.Parameters.GetProperty("scanner").GetProperty("maPeriod").GetInt32().ShouldBe(200);

        var first = await client.GetFromJsonAsync<StrategyDto>($"/api/strategies/{eu.Id}?version=1", ct);
        first!.Version.Universe.Count.ShouldBe(17);

        var stranger = await _factory.CreateSignedInClientAsync(ct);
        (await stranger.GetAsync($"/api/strategies/{eu.Id}", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object body, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(url, body, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));

        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }
}
