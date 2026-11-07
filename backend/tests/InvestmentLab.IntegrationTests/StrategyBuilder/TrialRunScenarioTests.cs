using System.Net;
using System.Net.Http.Json;
using InvestmentLab.Backtesting.Replays.Application.GetBacktest;
using InvestmentLab.IntegrationTests.Strategies;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;

namespace InvestmentLab.IntegrationTests.StrategyBuilder;

public sealed class TrialRunScenarioTests : IClassFixture<StrategiesApiFactory>
{
    private static readonly TimeSpan LongestReplay = TimeSpan.FromMinutes(5);
    private readonly StrategiesApiFactory _factory;

    public TrialRunScenarioTests(AppHostFixture appHost, StrategiesApiFactory factory)
    {
        _factory = factory;
        _factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Fact]
    public async Task Concurrent_trial_requests_reuse_one_version_and_changed_rules_create_the_next_run()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(cancellationToken);
        var ingestion = await client.PostAsync("/api/market-data/quotes/ingest", null, cancellationToken);
        ingestion.EnsureSuccessStatusCode();
        var draft = await ConfiguredDraftAsync(client, cancellationToken);

        var requests = Enumerable.Range(0, 2).Select(_ => RunAsync(client, draft.Id, cancellationToken));
        var repeated = await Task.WhenAll(requests);

        repeated[0].Number.ShouldBe(1);
        repeated[1].Number.ShouldBe(1);
        repeated[0].StrategyId.ShouldBe(repeated[1].StrategyId);
        repeated[0].Version.ShouldBe(repeated[1].Version);
        var strategies = await client.GetFromJsonAsync<List<StrategyDto>>("/api/strategies", cancellationToken);
        strategies!.ShouldNotContain(strategy => strategy.Id == repeated[0].StrategyId);

        var finished = await FinishedAsync(client, repeated[0], cancellationToken);
        finished.Status.ShouldBe("Succeeded", finished.FailureReason);
        var listed = await client.GetFromJsonAsync<List<TrialRunDto>>($"/api/strategy-builder/drafts/{draft.Id}/trials", cancellationToken);
        listed!.Count.ShouldBe(1);
        listed[0].Status.ShouldBe("Succeeded");
        listed[0].AnnualReturn.ShouldNotBeNull();

        var changed = await client.PutAsJsonAsync($"/api/strategy-builder/drafts/{draft.Id}", new
        {
            revision = draft.Revision,
            description = "A changed trial fingerprint keeps the first version immutable.",
            setup = draft.Setup with { Funds = [.. draft.Setup.Funds.Take(4)] },
        }, cancellationToken);
        changed.EnsureSuccessStatusCode();
        var second = await RunAsync(client, draft.Id, cancellationToken);

        second.Number.ShouldBe(2);
        second.StrategyId.ShouldBe(repeated[0].StrategyId);
        second.Version.ShouldBe(2);
        var afterChange = await client.GetFromJsonAsync<List<TrialRunDto>>($"/api/strategy-builder/drafts/{draft.Id}/trials", cancellationToken);
        afterChange!.Select(run => run.Version).ShouldBe([1, 2]);

        var rerun = await RunAsync(client, draft.Id, cancellationToken, rerun: true);

        rerun.Number.ShouldBe(3);
        rerun.Version.ShouldBe(3);
    }

    private static async Task<DraftDto> ConfiguredDraftAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var instruments = await client.GetFromJsonAsync<List<InstrumentChoiceDto>>("/api/strategy-builder/instruments", cancellationToken);
        var regular = instruments!.Where(instrument => !instrument.IsVolatilityIndex).Take(6).ToList();
        regular.Count.ShouldBe(6);
        var created = await client.PostAsJsonAsync("/api/strategy-builder/drafts", new { name = "Trial scenario", template = "momentum-trend-filter" }, cancellationToken);
        created.EnsureSuccessStatusCode();
        var draft = (await created.Content.ReadFromJsonAsync<DraftDto>(cancellationToken))!;
        var setup = new DraftSetupDto(
            Funds: [.. regular.Take(5).Select(instrument => instrument.Id)],
            BenchmarkId: regular[0].Id,
            CashId: regular[5].Id,
            VolatilityIndexId: null,
            ExtraSeries: []);
        var changed = await client.PutAsJsonAsync($"/api/strategy-builder/drafts/{draft.Id}", new { revision = draft.Revision, setup }, cancellationToken);
        changed.EnsureSuccessStatusCode();

        return (await changed.Content.ReadFromJsonAsync<DraftDto>(cancellationToken))!;
    }

    private static async Task<TrialRunDto> RunAsync(HttpClient client, Guid draftId, CancellationToken cancellationToken, bool rerun = false)
    {
        var response = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{draftId}/trials", new { label = "Trial", rerun }, cancellationToken);
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, detail);

        return (await response.Content.ReadFromJsonAsync<TrialRunDto>(cancellationToken))!;
    }

    private static async Task<BacktestDto> FinishedAsync(HttpClient client, TrialRunDto trial, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + LongestReplay;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/api/backtesting/strategies/{trial.StrategyId}?version={trial.Version}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var backtest = (await response.Content.ReadFromJsonAsync<BacktestDto>(cancellationToken))!;
                if (backtest.Status is not ("Queued" or "Running"))
                    return backtest;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException("The trial replay did not finish within five minutes.");
    }
}
