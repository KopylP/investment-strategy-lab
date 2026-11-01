using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using InvestmentLab.IntegrationTests.Strategies;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Api;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Application.GetReport;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.IntegrationTests.Rebalancing;

public sealed class RebalancingScenarioTests : IClassFixture<StrategiesApiFactory>
{
    private readonly StrategiesApiFactory _factory;

    public RebalancingScenarioTests(AppHostFixture appHost, StrategiesApiFactory factory)
    {
        _factory = factory;
        _factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Fact]
    public async Task Report_marks_reach_the_journal_and_can_be_undone()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(ct);
        (await client.PostAsync("/api/market-data/quotes/ingest", null, ct)).EnsureSuccessStatusCode();
        (await client.PostAsync("/api/market-data/fx-rates/ingest", null, ct)).EnsureSuccessStatusCode();
        var strategies = await client.DefaultStrategiesAsync<StrategyDto>(strategy => strategy.Name, ct);

        var account = await PostAsync<AccountSetupDto>(client, "/api/portfolio/accounts", new
        {
            name = "XTB EUR",
            broker = "XTB",
            baseCurrency = "EUR",
            subPortfolios = new object[]
            {
                new { name = "EU momentum", targetSharePercent = 70m, strategyId = strategies[0].Id },
                new { name = "US momentum", targetSharePercent = 30m, strategyId = strategies[1].Id },
            },
        }, ct);
        (await client.PutAsJsonAsync($"/api/rebalancing/accounts/{account.Id}/settings",
            new { monthlyContribution = 1000m, minTrade = 25m, trimBand = 0.1m, allowFractionalShares = true }, ct)).EnsureSuccessStatusCode();

        var asOf = SyntheticQuoteProvider.LastDay;
        var report = await ReportAsync(client, await RequestReportAsync(client, account.Id, new { asOf }, ct), ct);
        report.Summary.Status.ShouldBe("New");
        report.Summary.Trigger.ShouldBe("OnDemand");
        (report.Summary.Code, report.Summary.Title).ShouldBe(("rebalance", "@etf_momentum.report.rebalance.name"));

        var schedule = (await client.GetFromJsonAsync<ReportScheduleDto>($"/api/rebalancing/accounts/{account.Id}/report-schedule", ct))!;
        schedule.Reports.Select(each => (each.Code, each.Mode, each.Schedule.Frequency, each.Schedule.Day, each.Schedule.Weekday, each.Schedule.Time, each.Own)).Distinct()
            .ShouldBe([("rebalance", "rebalance", "Monthly", 1, "Sunday", "20:00", false), ("risk-review", "riskReview", "Weekly", 1, "Sunday", "19:00", false)]);

        var payday = await client.PutAsJsonAsync($"/api/rebalancing/accounts/{account.Id}/report-schedule/rebalance", new { frequency = "monthly", day = 10, time = "09:00" }, ct);
        payday.StatusCode.ShouldBe(HttpStatusCode.OK);
        var own = (await payday.Content.ReadFromJsonAsync<ReportScheduleDto>(ct))!.Reports.Single(each => each.Code == "rebalance");
        (own.Own, own.Schedule.Day, own.Schedule.Time, own.StrategySchedule.Day, own.Next![8..]).ShouldBe((true, 10, "09:00", 1, "10T09:00"));
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var next = await scope.ServiceProvider.GetRequiredService<IReportScheduleQueries>().NextAsync(account.Id, "rebalance", ct);
            (next?.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)).ShouldBe(own.Next);
        }

        (await client.PutAsJsonAsync($"/api/rebalancing/accounts/{account.Id}/report-schedule/rebalance", new { frequency = "monthly", day = 32, time = "09:00" }, ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PutAsJsonAsync($"/api/rebalancing/accounts/{account.Id}/report-schedule/weekly-digest", new { frequency = "off" }, ct))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var reset = await client.DeleteAsync($"/api/rebalancing/accounts/{account.Id}/report-schedule/rebalance", ct);
        (await reset.Content.ReadFromJsonAsync<ReportScheduleDto>(ct))!.Reports.Single(each => each.Code == "rebalance").Own.ShouldBeFalse();
        var review = await RequestReportAsync(client, account.Id, new { code = "risk-review", asOf }, ct);
        (await ReportAsync(client, review, ct)).Summary.ShouldSatisfyAllConditions(
            summary => summary.Kind.ShouldBe("RiskReview"), summary => summary.Title.ShouldBe("@etf_momentum.report.risk_review.name"));
        (await client.PostAsJsonAsync($"/api/rebalancing/accounts/{account.Id}/reports", new { code = "weekly-digest" }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        report.Sections.Count.ShouldBe(2);
        report.Sections.ShouldAllBe(section => section.Error == null);
        report.Warnings.ShouldContain(warning => warning.Contains("report.warning.fx_converted"));
        var contribute = report.Actions[0];
        contribute.Kind.ShouldBe("Contribute");
        contribute.PlannedAmount.ShouldBe(1000m);
        contribute.Allocations.Sum(allocation => allocation.Quantity).ShouldBe(1000m);
        var buy = report.Actions.First(action => action.Kind == "Buy");
        report.Actions.Where(action => action.Kind is "Buy" or "Park").Sum(action => action.PlannedAmount).ShouldBe(1000m, 0.05m);

        (await client.PostAsJsonAsync($"/api/rebalancing/reports/{report.Summary.Id}/actions/{contribute.Id}/fills", new { }, ct)).EnsureSuccessStatusCode();
        await EventuallyAsync(async () => (await HoldingsAsync(client, account.Id, ct)).SubPortfolios.Sum(sub => sub.Cash) == 1000m, ct);

        var half = decimal.Round(buy.PlannedQuantity / 2m, 4);
        (await client.PostAsJsonAsync($"/api/rebalancing/reports/{report.Summary.Id}/actions/{buy.Id}/partial-fills",
            new { quantity = half, price = buy.PlannedPrice * 0.99m, fee = 1m }, ct)).EnsureSuccessStatusCode();
        await EventuallyAsync(async () => (await ReportAsync(client, report.Summary.Id, ct)).Actions.First(a => a.Id == buy.Id).Fills.Single().TransactionId is not null, ct);

        report = await ReportAsync(client, report.Summary.Id, ct);
        report.Summary.Status.ShouldBe("InProgress");
        report.Actions.First(action => action.Id == buy.Id).Status.ShouldBe("PartiallyExecuted");
        var holdings = await HoldingsAsync(client, account.Id, ct);
        var sleeve = buy.Allocations.Single().SubPortfolioId;
        holdings.SubPortfolios.Single(sub => sub.SubPortfolioId == sleeve).Positions.Single(p => p.InstrumentId == buy.InstrumentId).Quantity.ShouldBe(half);

        (await client.PostAsync($"/api/rebalancing/reports/{report.Summary.Id}/actions/{buy.Id}/undo", null, ct)).EnsureSuccessStatusCode();
        await EventuallyAsync(async () => (await HoldingsAsync(client, account.Id, ct)).SubPortfolios.All(sub => sub.Positions.Count == 0), ct);
        (await ReportAsync(client, report.Summary.Id, ct)).Actions.First(action => action.Id == buy.Id).Status.ShouldBe("Pending");

        var again = await RequestReportAsync(client, account.Id, new { asOf }, ct);
        (await ReportAsync(client, report.Summary.Id, ct)).Summary.Status.ShouldBe("Superseded");
        var replacement = await ReportAsync(client, again, ct);
        replacement.Actions.ShouldNotContain(action => action.Kind == "Contribute");
        replacement.Actions.Where(action => action.Kind is "Buy" or "Park").Sum(action => action.PlannedAmount).ShouldBe(1000m, 0.05m);

        var stranger = await _factory.CreateSignedInClientAsync(ct);
        (await stranger.GetAsync($"/api/rebalancing/reports/{again}", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.GetFromJsonAsync<List<ReportRequestDto>>("/api/rebalancing/report-requests", ct))!.ShouldBeEmpty();
    }

    private static async Task<Guid> RequestReportAsync(HttpClient client, Guid accountId, object body, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"/api/rebalancing/accounts/{accountId}/reports", body, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync(ct));
        var requested = (await response.Content.ReadFromJsonAsync<ReportRequestedResponse>(ct))!;
        response.Headers.Location!.ToString().ShouldBe($"/api/rebalancing/report-requests/{requested.RequestId}");
        (await client.GetFromJsonAsync<List<ReportRequestDto>>($"/api/rebalancing/report-requests?accountId={accountId}", ct))!
            .ShouldContain(request => request.Id == requested.RequestId);

        ReportRequestDto? request = null;
        await EventuallyAsync(async () =>
        {
            request = await client.GetFromJsonAsync<ReportRequestDto>($"/api/rebalancing/report-requests/{requested.RequestId}", ct);

            return request!.Status != "Pending";
        }, ct);
        request!.Status.ShouldBe("Completed", request.Error);
        request.StrategiesAnswered.ShouldBe(request.Strategies);

        return request.ReportId!.Value;
    }

    private static async Task<ReportDetailsDto> ReportAsync(HttpClient client, Guid reportId, CancellationToken ct) =>
        (await client.GetFromJsonAsync<ReportDetailsDto>($"/api/rebalancing/reports/{reportId}", ct))!;

    private static async Task<AccountHoldingsDto> HoldingsAsync(HttpClient client, Guid accountId, CancellationToken ct) =>
        (await client.GetFromJsonAsync<AccountHoldingsDto>($"/api/portfolio/accounts/{accountId}/holdings", ct))!;

    private static async Task EventuallyAsync(Func<Task<bool>> condition, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (!await condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("The condition did not become true within 30 seconds.");

            await Task.Delay(250, ct);
        }
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string url, object body, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync(url, body, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));

        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }
}
