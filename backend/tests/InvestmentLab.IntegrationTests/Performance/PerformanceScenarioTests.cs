using System.Net;
using System.Net.Http.Json;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.IntegrationTests.Portfolio;
using InvestmentLab.Performance.Valuation.Application;

namespace InvestmentLab.IntegrationTests.Performance;

public sealed class PerformanceScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    [Fact]
    public async Task A_change_back_in_time_rebuilds_snapshots_and_the_time_weighted_return()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsync("/api/market-data/quotes/ingest", null, ct)).EnsureSuccessStatusCode();
        var instruments = await client.GetFromJsonAsync<List<InstrumentRow>>("/api/market-data/instruments", ct);
        var sxr8 = instruments!.Single(instrument => instrument.ProviderSymbols.Contains("yfinance:SXR8.DE")).Id;
        var days = FakeQuoteProvider.TradingDays();

        var account = await PortfolioScenarioTests.OpenTwoSleeveAccountAsync(client, ct);
        var sleeve = account.SubPortfolios[0].Id;
        await PostAsync(client, $"/api/portfolio/accounts/{account.Id}/contributions", new { amount = 1000m, occurredOn = days[0], allocations = new[] { new { subPortfolioId = sleeve, amount = 1000m } } }, ct);
        await PostAsync(client, $"/api/portfolio/accounts/{account.Id}/trades",
            new { instrumentId = sxr8, side = "Buy", quantity = 10m, price = 100m, fee = 0m, executedOn = days[0], allocations = new[] { new { subPortfolioId = sleeve, quantity = 10m } } }, ct);

        var first = await DashboardAsync(client, account.Id, dashboard => dashboard.Value == 1140m, ct);
        first.NetContributions.ShouldBe(1000m);
        first.Twr!.Value.ShouldBe(0.14m, 0.000001m);
        first.Series.Count.ShouldBe(15);
        first.MaxDrawdown.ShouldBe(0m);
        first.SubPortfolios.Single(sub => sub.Id == sleeve).ActualShare.ShouldBe(1m);

        await PostAsync(client, $"/api/portfolio/accounts/{account.Id}/contributions", new { amount = 500m, occurredOn = days[5], allocations = new[] { new { subPortfolioId = sleeve, amount = 500m } } }, ct);

        var rebuilt = await DashboardAsync(client, account.Id, dashboard => dashboard.Value == 1640m, ct);
        rebuilt.NetContributions.ShouldBe(1500m);
        rebuilt.Twr!.Value.ShouldBe(1.04m * (1550m / 1540m) * (1640m / 1550m) - 1m, 0.000001m);
        rebuilt.Series[5].NetContributions.ShouldBe(1500m);
        rebuilt.Xirr.ShouldNotBeNull();

        var other = await PortfolioScenarioTests.OpenTwoSleeveAccountAsync(client, ct);
        await PostAsync(client, $"/api/portfolio/accounts/{other.Id}/contributions",
            new { amount = 360m, occurredOn = days[0], allocations = new[] { new { subPortfolioId = other.SubPortfolios[0].Id, amount = 360m } } }, ct);
        var all = await client.GetFromJsonAsync<DashboardDto>("/api/performance/dashboard?currency=EUR", ct);
        all!.AccountId.ShouldBeNull();
        all.Value.ShouldBe(2000m);
        all.NetContributions.ShouldBe(1860m);
        all.Series[0].Value.ShouldBe(1000m + 360m);
        all.Accounts.Select(a => a.Value).ShouldBe([1640m, 360m], ignoreOrder: true);
        all.Accounts.Sum(a => a.Share).ShouldBe(1m);

        var stranger = await factory.CreateSignedInClientAsync(ct);
        (await stranger.GetAsync($"/api/performance/accounts/{account.Id}/dashboard", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.GetFromJsonAsync<DashboardDto>("/api/performance/dashboard", ct))!.Accounts.ShouldBeEmpty();
    }

    private static async Task PostAsync(HttpClient client, string url, object body, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(url, body, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<DashboardDto> DashboardAsync(HttpClient client, Guid accountId, Func<DashboardDto, bool> condition, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var response = await client.GetAsync($"/api/performance/accounts/{accountId}/dashboard", ct);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));
            var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>(ct);
            if (dashboard is not null && condition(dashboard))
                return dashboard;

            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Dashboard did not reach the expected state: value {dashboard?.Value}.");

            await Task.Delay(300, ct);
        }
    }

    private sealed record InstrumentRow(Guid Id, IReadOnlyList<string> ProviderSymbols);
}
