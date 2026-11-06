using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.Backtesting.Replays.Application.GetBacktest;
using InvestmentLab.IntegrationTests.Strategies;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListDrafts;
using InvestmentLab.StrategyBuilder.Drafts.Application.PublishDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;

namespace InvestmentLab.IntegrationTests.StrategyBuilder;

public sealed class StrategyBuilderScenarioTests : IClassFixture<StrategiesApiFactory>
{
    private static readonly TimeSpan LongestReplay = TimeSpan.FromMinutes(5);
    private readonly StrategiesApiFactory _factory;

    public StrategyBuilderScenarioTests(AppHostFixture appHost, StrategiesApiFactory factory)
    {
        _factory = factory;
        _factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Fact]
    public async Task A_draft_is_backtested_published_and_leaves_no_trial_behind()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(cancellationToken);
        var ingestion = await client.PostAsync("/api/market-data/quotes/ingest", null, cancellationToken);
        ingestion.EnsureSuccessStatusCode();
        var name = $"Lifecycle {Guid.CreateVersion7()}";
        var draft = await ConfiguredDraftAsync(client, name, cancellationToken);

        var trial = await RunAsync(client, draft.Id, cancellationToken);
        var repeated = await RunAsync(client, draft.Id, cancellationToken);
        repeated.Number.ShouldBe(1);
        repeated.StrategyId.ShouldBe(trial.StrategyId);
        repeated.Version.ShouldBe(trial.Version);
        var strategies = await client.GetFromJsonAsync<List<StrategyDto>>("/api/strategies", cancellationToken);
        strategies!.ShouldNotContain(strategy => strategy.Id == trial.StrategyId);
        var finished = await FinishedAsync(client, trial, cancellationToken);
        finished.Status.ShouldBe("Succeeded", finished.FailureReason);

        var published = await PublishAsync(client, draft, cancellationToken);

        published.Version.ShouldBe(1);
        var drafts = await client.GetFromJsonAsync<List<DraftSummaryDto>>("/api/strategy-builder/drafts", cancellationToken);
        drafts!.ShouldBeEmpty();
        await TrialDisappearsAsync(client, trial, cancellationToken);
        var publishedRun = trial with { StrategyId = published.StrategyId, Version = published.Version };
        var publishedBacktest = await FinishedAsync(client, publishedRun, cancellationToken);
        publishedBacktest.Status.ShouldBe("Succeeded", publishedBacktest.FailureReason);

        var reopened = await OpenAsync(client, published, cancellationToken);
        var again = await OpenAsync(client, published, cancellationToken);
        again.Id.ShouldBe(reopened.Id);
        var duplicate = await ConfiguredDraftAsync(client, name, cancellationToken);
        var duplicateTrial = await RunAsync(client, duplicate.Id, cancellationToken);
        var refused = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{duplicate.Id}/publish", new { revision = duplicate.Revision }, cancellationToken);
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("strategy.name_taken");
        (await client.GetAsync($"/api/strategy-builder/drafts/{duplicate.Id}", cancellationToken)).EnsureSuccessStatusCode();
        var retained = await client.GetFromJsonAsync<List<TrialRunDto>>($"/api/strategy-builder/drafts/{duplicate.Id}/trials", cancellationToken);
        retained!.Single().StrategyId.ShouldBe(duplicateTrial.StrategyId);
    }

    private static async Task TrialDisappearsAsync(HttpClient client, TrialRunDto trial, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync($"/api/backtesting/strategies/{trial.StrategyId}?version={trial.Version}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return;

            response.EnsureSuccessStatusCode();
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException("The deleted trial backtest remained visible after thirty seconds.");
    }

    private static async Task<DraftDto> ConfiguredDraftAsync(HttpClient client, string name, CancellationToken cancellationToken)
    {
        var instruments = await client.GetFromJsonAsync<List<InstrumentChoiceDto>>("/api/strategy-builder/instruments", cancellationToken);
        var regular = instruments!.Where(instrument => !instrument.IsVolatilityIndex).Take(6).ToList();
        var created = await client.PostAsJsonAsync("/api/strategy-builder/drafts", new { name, template = "momentum-trend-filter" }, cancellationToken);
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

    private static async Task<PublishedVersionDto> PublishAsync(HttpClient client, DraftDto draft, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{draft.Id}/publish", new { revision = draft.Revision }, cancellationToken);
        var details = await response.Content.ReadAsStringAsync(cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, details);

        return (await response.Content.ReadFromJsonAsync<PublishedVersionDto>(cancellationToken))!;
    }

    private static async Task<DraftDto> OpenAsync(HttpClient client, PublishedVersionDto published, CancellationToken cancellationToken)
    {
        var response = await client.PostAsync($"/api/strategy-builder/published/{published.StrategyId}/versions/{published.Version}/draft", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DraftDto>(cancellationToken))!;
    }

    private static async Task<TrialRunDto> RunAsync(HttpClient client, Guid draftId, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{draftId}/trials", new { label = "Trial" }, cancellationToken);
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
