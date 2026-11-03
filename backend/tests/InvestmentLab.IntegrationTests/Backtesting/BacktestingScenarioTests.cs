using System.Net.Http.Json;
using InvestmentLab.Backtesting.Replays.Application.GetBacktest;
using InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.IntegrationTests.Strategies;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.IntegrationTests.Backtesting;

public sealed class BacktestingScenarioTests : IClassFixture<StrategiesApiFactory>
{
    private static readonly TimeSpan LongestReplay = TimeSpan.FromMinutes(5);
    private readonly StrategiesApiFactory _factory;

    public BacktestingScenarioTests(AppHostFixture appHost, StrategiesApiFactory factory)
    {
        _factory = factory;
        _factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Theory]
    [InlineData("EU index hold")]
    [InlineData("EU adaptive momentum")]
    public async Task Any_strategy_type_is_replayed_in_the_background_and_gives_money_outcomes(string name)
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(ct);
        (await client.PostAsync("/api/market-data/quotes/ingest", null, ct)).EnsureSuccessStatusCode();
        var strategies = await client.DefaultStrategiesAsync<StrategyDto>(strategy => strategy.Name, ct);
        var strategy = strategies.Single(candidate => candidate.Name == name);

        var finished = await FinishedAsync(client, strategy.Id, ct);

        finished.Status.ShouldBe(nameof(BacktestStatus.Succeeded), finished.FailureReason);
        finished.Result!.Trades.ShouldBeGreaterThan(0);
        finished.Result.Series[0].Code.ShouldBe(ReplaySeries.StrategyCode);
        finished.Result.Series.ShouldContain(series => series.Code == "benchmark");

        var outcomes = await client.GetFromJsonAsync<MoneyOutcomesDto>(
            $"/api/backtesting/strategies/{strategy.Id}/money-outcomes?version={finished.StrategyVersion}&start=1000&contribution=100&years=1", ct);

        outcomes!.Contributed.ShouldBe(2200m);
        outcomes.Series.Select(series => series.Code).ShouldBe(finished.Result.Series.Select(series => series.Code));
    }

    [Fact]
    public async Task Changing_the_parameters_queues_a_replay_of_the_new_version()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(ct);
        var strategies = await client.DefaultStrategiesAsync<StrategyDto>(strategy => strategy.Name, ct);
        var strategy = strategies.Single(candidate => candidate.Name == "EU index hold");
        var version = strategy.Version;

        var changed = await client.PutAsJsonAsync($"/api/strategies/{strategy.Id}", new
        {
            universe = version.Universe,
            benchmarkInstrumentId = version.BenchmarkInstrumentId,
            volatilityIndexInstrumentId = version.VolatilityIndexInstrumentId,
            cashInstrumentId = version.CashInstrumentId,
            parameters = version.Parameters,
        }, ct);
        changed.EnsureSuccessStatusCode();

        await WaitUntilAsync(() => BacktestExistsAsync(strategy.Id, version.Number + 1, ct), ct);
    }

    private async Task<bool> BacktestExistsAsync(Guid strategyId, int version, CancellationToken ct)
    {
        await using var scope = _factory.Services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<IBacktestRepository>().FindAsync(strategyId, version, ct) is not null;
    }

    private static async Task<BacktestDto> FinishedAsync(HttpClient client, Guid strategyId, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + LongestReplay;

        while (true)
        {
            var backtest = await client.GetFromJsonAsync<BacktestDto>($"/api/backtesting/strategies/{strategyId}", ct);

            if (backtest!.Status is not (nameof(BacktestStatus.Queued) or nameof(BacktestStatus.Running)) || DateTime.UtcNow > deadline)
                return backtest;

            await Task.Delay(1000, ct);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (!await condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("The condition did not become true within 30 seconds.");

            await Task.Delay(250, ct);
        }
    }
}
