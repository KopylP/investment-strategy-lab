using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.IntegrationTests.Strategies;
using InvestmentLab.StrategyBuilder.Drafts.Application.CleanUpTrials;
using InvestmentLab.StrategyBuilder.Drafts.Application.GetPublishedStrategy;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Drafts.Application.PublishDraft;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Shared.Infrastructure;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.IntegrationTests.StrategyBuilder;

public sealed class PublishedDraftScenarioTests : IClassFixture<StrategiesApiFactory>
{
    private readonly StrategiesApiFactory _factory;

    public PublishedDraftScenarioTests(AppHostFixture appHost, StrategiesApiFactory factory)
    {
        _factory = factory;
        _factory.StrategyEngineUrl ??= appHost.App.GetEndpoint("strategy-engine", "http").ToString();
    }

    [Fact]
    public async Task Publishing_preserves_the_layout_and_reopening_keeps_one_draft_for_the_next_version()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = await _factory.CreateSignedInClientAsync(cancellationToken);
        var name = $"Published {Guid.CreateVersion7()}";
        var draft = await ConfiguredDraftAsync(client, name, cancellationToken);
        var nodeId = draft.Graph.Nodes[0].Id;
        var layout = new LayoutDto(new Dictionary<string, NodePositionDto> { [nodeId] = new(32, 64) }, ["Risk"]);
        var moved = await client.PutAsJsonAsync($"/api/strategy-builder/drafts/{draft.Id}/layout", layout, cancellationToken);
        moved.EnsureSuccessStatusCode();

        var published = await PublishAsync(client, draft, cancellationToken);

        published.Version.ShouldBe(1);
        (await client.GetAsync($"/api/strategy-builder/drafts/{draft.Id}", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var shown = await client.GetFromJsonAsync<PublishedStrategyDto>($"/api/strategy-builder/published/{published.StrategyId}/versions/1", cancellationToken);
        shown!.Editable.ShouldBeTrue();
        shown.Words.ShouldNotBeEmpty();
        var reopened = await OpenAsync(client, published, cancellationToken);
        var again = await OpenAsync(client, published, cancellationToken);
        again.Id.ShouldBe(reopened.Id);
        reopened.Graph.Nodes.Select(node => node.Id).ShouldBe(draft.Graph.Nodes.Select(node => node.Id));
        reopened.Layout.Positions[nodeId].ShouldBe(new NodePositionDto(32, 64));
        reopened.Layout.Collapsed.ShouldBe(["Risk"]);
        reopened.OriginStrategyId.ShouldBe(published.StrategyId);

        var next = await PublishAsync(client, reopened, cancellationToken);
        next.StrategyId.ShouldBe(published.StrategyId);
        next.Version.ShouldBe(2);
        var duplicate = await ConfiguredDraftAsync(client, name, cancellationToken);
        var refused = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{duplicate.Id}/publish", new { revision = duplicate.Revision }, cancellationToken);
        refused.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetProperty("code").GetString().ShouldBe("strategy.name_taken");
        (await client.GetAsync($"/api/strategy-builder/drafts/{duplicate.Id}", cancellationToken)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_failed_trial_cleanup_keeps_the_published_strategy_and_is_retried_afterwards()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var failure = new TrialDeletionFailure();
        await using var faultedFactory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            var original = services.Single(service => service.ServiceType == typeof(IStrategyAuthoring));
            services.Remove(original);
            services.AddScoped<IStrategyAuthoring>(provider =>
            {
                var inner = (IStrategyAuthoring)ActivatorUtilities.CreateInstance(provider, original.ImplementationType!);

                return new FaultedTrialAuthoring(inner, failure);
            });
        }));
        var client = await faultedFactory.CreateSignedInClientAsync(cancellationToken);
        var draft = await ConfiguredDraftAsync(client, $"Cleanup {Guid.CreateVersion7()}", cancellationToken);
        var trialResponse = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{draft.Id}/trials", new { label = "Cleanup trial" }, cancellationToken);
        trialResponse.EnsureSuccessStatusCode();
        var trial = (await trialResponse.Content.ReadFromJsonAsync<TrialRunDto>(cancellationToken))!;

        var published = await PublishAsync(client, draft, cancellationToken);

        (await client.GetAsync($"/api/strategies/{published.StrategyId}", cancellationToken)).EnsureSuccessStatusCode();
        (await client.GetAsync($"/api/strategy-builder/drafts/{draft.Id}", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        Guid cleanupId;
        await using (var scope = faultedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StrategyBuilderDbContext>();
            var cleanup = await db.TrialCleanups.SingleAsync(row => row.TrialStrategyId == trial.StrategyId, cancellationToken);
            cleanup.Attempts.ShouldBe(1);
            cleanup.NextAttemptAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
            cleanupId = cleanup.Id;
        }

        failure.Refuse = false;
        await using (var scope = faultedFactory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<CleanUpTrialHandler>().Handle(cleanupId, cancellationToken);

        await using (var scope = faultedFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StrategyBuilderDbContext>();
            (await db.TrialCleanups.AnyAsync(row => row.Id == cleanupId, cancellationToken)).ShouldBeFalse();
        }
        (await client.GetAsync($"/api/strategies/{trial.StrategyId}", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var discarded = await ConfiguredDraftAsync(client, $"Discard {Guid.CreateVersion7()}", cancellationToken);
        var discardTrialResponse = await client.PostAsJsonAsync($"/api/strategy-builder/drafts/{discarded.Id}/trials", new { label = "Discard trial" }, cancellationToken);
        discardTrialResponse.EnsureSuccessStatusCode();
        var discardTrial = (await discardTrialResponse.Content.ReadFromJsonAsync<TrialRunDto>(cancellationToken))!;
        (await client.DeleteAsync($"/api/strategy-builder/drafts/{discarded.Id}", cancellationToken)).EnsureSuccessStatusCode();
        await using (var scope = faultedFactory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<TrialCleanupJob>().RunAsync(cancellationToken);

        (await client.GetAsync($"/api/strategies/{discardTrial.StrategyId}", cancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
}
