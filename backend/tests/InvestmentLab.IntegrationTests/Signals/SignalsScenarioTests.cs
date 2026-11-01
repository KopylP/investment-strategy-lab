using System.Net;
using System.Net.Http.Json;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Signals.Monitoring.Application.CreateSignal;
using InvestmentLab.Signals.Monitoring.Application.GetSignals;
using InvestmentLab.Signals.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.IntegrationTests.Signals;

public sealed class SignalsScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    [Fact]
    public async Task A_signal_is_stored_fires_once_into_the_outbox_and_is_deleted_with_its_events()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsync("/api/market-data/quotes/ingest", null, ct)).EnsureSuccessStatusCode();
        var instrumentId = await Sp500IdAsync(client, ct);

        var created = await CreateLevelSignalAsync(client, instrumentId, ct);

        var stored = await SignalAsync(client, created.Id, ct);
        stored.Target.ShouldBe("Instrument");
        stored.InstrumentId.ShouldBe(instrumentId);
        stored.Condition.Threshold.ShouldBe(50m);
        stored.Condition.RearmAt.ShouldBe(40m);
        (stored.Schedule.Frequency, stored.Schedule.Weekday, stored.Schedule.Time).ShouldBe(("Weekly", "Friday", "18:30"));

        await CheckAsync(client, created.Id, ct);
        await CheckAsync(client, created.Id, ct);

        var overview = await OverviewAsync(client, ct);
        var alarm = overview.Active.ShouldHaveSingleItem();
        alarm.SignalId.ShouldBe(created.Id);
        alarm.SubjectLabel.ShouldBe(FakeQuoteProvider.Symbol);

        var fired = await OutboxPayloadsAsync("SignalTriggeredV1", created.Id, ct);
        fired.ShouldHaveSingleItem().ShouldContain(FakeQuoteProvider.Symbol);

        (await client.DeleteAsync($"/api/signals/{created.Id}", ct)).EnsureSuccessStatusCode();

        (await OutboxPayloadsAsync("SignalClearedV1", created.Id, ct)).ShouldHaveSingleItem().ShouldContain("Deleted");
        (await OutboxPayloadsAsync("SignalDeletedV1", created.Id, ct)).ShouldHaveSingleItem();
        (await OverviewAsync(client, ct)).Active.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_impossible_signal_is_refused_and_stores_nothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);

        var response = await client.PostAsJsonAsync("/api/signals", new
        {
            name = "Без об'єкта",
            target = "Instrument",
            condition = new { metric = "Level", threshold = 50m },
            schedule = new { frequency = "Off" },
        }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await OverviewAsync(client, ct)).Signals.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_signal_on_a_group_of_instruments_is_stored_and_checked_and_an_unknown_group_is_refused()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        var instruments = await client.GetFromJsonAsync<List<InstrumentDto>>("/api/market-data/instruments", ct);
        var groupName = instruments!.First(instrument => instrument.Group is not null).Group!;

        var refused = await PostGroupSignalAsync(client, "No such group", ct);
        refused.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var response = await PostGroupSignalAsync(client, groupName, ct);
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<SignalCreatedDto>(ct))!;

        await CheckAsync(client, created.Id, ct);

        var stored = await SignalAsync(client, created.Id, ct);
        stored.Target.ShouldBe("InstrumentGroup");
        stored.GroupName.ShouldBe(groupName);
        stored.InstrumentId.ShouldBeNull();
        stored.LastCheckedAt.ShouldNotBeNull();
    }

    private static Task<HttpResponseMessage> PostGroupSignalAsync(HttpClient client, string groupName, CancellationToken ct) =>
        client.PostAsJsonAsync("/api/signals", new
        {
            name = "Падіння групи",
            target = "InstrumentGroup",
            groupName,
            condition = new { metric = "DrawdownFromPeak", direction = "Above", threshold = 0.2m },
            schedule = new { frequency = "Off" },
        }, ct);

    private static async Task<Guid> Sp500IdAsync(HttpClient client, CancellationToken ct)
    {
        var instruments = await client.GetFromJsonAsync<List<InstrumentDto>>("/api/market-data/instruments", ct);

        return instruments!.Single(instrument => instrument.ProviderSymbols.Contains($"yfinance:{FakeQuoteProvider.Symbol}")).Id;
    }

    private static async Task<SignalCreatedDto> CreateLevelSignalAsync(HttpClient client, Guid instrumentId, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/signals", new
        {
            name = "Ціна вище 50",
            target = "Instrument",
            instrumentId,
            condition = new { metric = "Level", direction = "Above", threshold = 50m, rearmAt = 40m },
            schedule = new { frequency = "Weekly", weekday = "Friday", time = "18:30" },
        }, ct);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SignalCreatedDto>(ct))!;
    }

    private static async Task CheckAsync(HttpClient client, Guid signalId, CancellationToken ct) =>
        (await client.PostAsync($"/api/signals/{signalId}/check", null, ct)).EnsureSuccessStatusCode();

    private static async Task<SignalsOverviewDto> OverviewAsync(HttpClient client, CancellationToken ct) =>
        (await client.GetFromJsonAsync<SignalsOverviewDto>("/api/signals", ct))!;

    private static async Task<SignalDto> SignalAsync(HttpClient client, Guid signalId, CancellationToken ct)
    {
        var overview = await OverviewAsync(client, ct);

        return overview.Signals.Single(signal => signal.Id == signalId);
    }

    private async Task<List<string>> OutboxPayloadsAsync(string eventName, Guid signalId, CancellationToken ct)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SignalsDbContext>();

        var messages = await db.Set<OutboxMessage>().ToListAsync(ct);

        return [.. messages
            .Where(message => message.Type.EndsWith(eventName, StringComparison.Ordinal))
            .Where(message => message.Payload.Contains(signalId.ToString(), StringComparison.Ordinal))
            .Select(message => message.Payload)];
    }
}
