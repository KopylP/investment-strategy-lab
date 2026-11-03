using System.Net;
using System.Net.Http.Json;
using InvestmentLab.IntegrationTests.Strategies;
using InvestmentLab.Signals.Monitoring.Application.CreateSignal;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListOwnSignals;
using PublishedVersionDto = InvestmentLab.StrategyBuilder.Drafts.Application.PublishDraft.PublishedVersionDto;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.IntegrationTests.StrategyBuilder;

public sealed class UserSignalScenarioTests : IClassFixture<StrategiesApiFactory>
{
    private readonly StrategiesApiFactory _factory;

    public UserSignalScenarioTests(AppHostFixture appHost, StrategiesApiFactory factory)
    {
        _factory = factory;
        factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Deleting_a_signal_blocks_its_draft_and_warns_its_strategy_but_only_breaks_evaluation_when_used_in_logic(bool usedInLogic)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(cancellationToken);
        var instruments = await client.GetFromJsonAsync<List<InstrumentChoiceDto>>("/api/strategy-builder/instruments", cancellationToken);
        var fund = instruments!.First(instrument => !instrument.IsVolatilityIndex).Id;
        (await client.PostAsync("/api/market-data/quotes/ingest", null, cancellationToken)).EnsureSuccessStatusCode();
        var signalId = await CreateSignalAsync(client, fund, cancellationToken);
        var own = await client.GetFromJsonAsync<List<OwnSignalDto>>("/api/strategy-builder/signals", cancellationToken);
        own!.ShouldContain(signal => signal.Id == signalId && signal.IsMarket);
        var draft = await DraftAsync(client, signalId, fund, usedInLogic, cancellationToken);
        var published = await PublishAsync(client, draft, cancellationToken);
        var reopened = await OpenAsync(client, published, cancellationToken);

        var before = await EvaluateAsync(published.StrategyId, cancellationToken);
        before.IsSuccess.ShouldBeTrue(before.ErrorMessage);
        before.Value!.Market.IsFavorable.ShouldBeTrue();
        (await client.DeleteAsync($"/api/signals/{signalId}", cancellationToken)).EnsureSuccessStatusCode();
        await WarningAppearsAsync(client, published.StrategyId, signalId, cancellationToken);

        var after = await EvaluateAsync(published.StrategyId, cancellationToken);
        if (usedInLogic)
        {
            after.IsSuccess.ShouldBeFalse();
            after.ErrorCode.ShouldBe("strategy.signal_deleted");
            after.ErrorMessage.ShouldStartWith("@strategy.signal_deleted");
        }
        else
            after.IsSuccess.ShouldBeTrue(after.ErrorMessage);

        var checkedDraft = await client.GetFromJsonAsync<DraftDto>($"/api/strategy-builder/drafts/{reopened.Id}", cancellationToken);
        checkedDraft!.Problems.ShouldContain(problem => problem.Message.StartsWith("@builder.problem.signal_deleted", StringComparison.Ordinal));
        var refused = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{reopened.Id}/publish", new { revision = reopened.Revision }, cancellationToken);
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private async Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(Guid strategyId, CancellationToken cancellationToken)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var evaluation = scope.ServiceProvider.GetRequiredService<IStrategyEvaluation>();
        var state = new SubPortfolioStateDto("EUR", 1000m, []);
        var request = new EvaluateStrategyRequest(strategyId, 1, state, 0m, SyntheticQuoteProvider.LastDay);

        return await evaluation.EvaluateAsync(request, cancellationToken);
    }

    private static async Task<Guid> CreateSignalAsync(HttpClient client, Guid instrumentId, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync("/api/signals", new
        {
            name = "Market signal",
            target = "Instrument",
            instrumentId,
            condition = new { metric = "Level", direction = "Above", threshold = 1m, rearmAt = 0.5m },
            schedule = new { frequency = "Off" },
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<SignalCreatedDto>(cancellationToken))!;
        (await client.PostAsync($"/api/signals/{created.Id}/check", null, cancellationToken)).EnsureSuccessStatusCode();

        return created.Id;
    }

    private static async Task<DraftDto> DraftAsync(HttpClient client, Guid signalId, Guid fund, bool usedInLogic, CancellationToken cancellationToken)
    {
        var marketFilter = usedInLogic
            ? $$""", "marketFilter":{"condition":{"kind":"signal","signalId":"{{signalId}}","state":"active","scope":"anyInstrument"},"investWhenUnfavorable":0}"""
            : "";
        var json = $$"""{"formatVersion":1,"indicators":{},"selection":{"order":"desc","top":1},"weights":{"method":"equal"}{{marketFilter}}}""";
        var imported = await client.PostAsJsonAsync("/api/strategy-builder/drafts/import", new { name = $"Signal {Guid.NewGuid()}", json }, cancellationToken);
        imported.EnsureSuccessStatusCode();
        var draft = (await imported.Content.ReadFromJsonAsync<DraftDto>(cancellationToken))!;
        var reports = draft.Reports.Select(report => report with { SignalIds = [signalId] }).ToList();
        var setup = new DraftSetupDto([fund], fund, fund, null, []);
        var changed = await client.PutAsJsonAsync($"/api/strategy-builder/drafts/{draft.Id}", new { revision = draft.Revision, setup, reports }, cancellationToken);
        changed.EnsureSuccessStatusCode();

        return (await changed.Content.ReadFromJsonAsync<DraftDto>(cancellationToken))!;
    }

    private static async Task<PublishedVersionDto> PublishAsync(HttpClient client, DraftDto draft, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{draft.Id}/publish", new { revision = draft.Revision }, cancellationToken);
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, detail);

        return (await response.Content.ReadFromJsonAsync<PublishedVersionDto>(cancellationToken))!;
    }

    private static async Task<DraftDto> OpenAsync(HttpClient client, PublishedVersionDto published, CancellationToken cancellationToken)
    {
        var response = await client.PostAsync($"/api/strategy-builder/published/{published.StrategyId}/versions/1/draft", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<DraftDto>(cancellationToken))!;
    }

    private static async Task WarningAppearsAsync(HttpClient client, Guid strategyId, Guid signalId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var strategy = await client.GetFromJsonAsync<StrategyDto>($"/api/strategies/{strategyId}", cancellationToken);
            if (strategy!.DeletedSignalIds.Contains(signalId))
                return;

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException("The strategy did not receive the deleted signal event.");
    }
}
