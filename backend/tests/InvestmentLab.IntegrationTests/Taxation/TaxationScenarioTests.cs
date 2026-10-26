using System.Net;
using System.Net.Http.Json;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.IntegrationTests.Portfolio;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Application;

namespace InvestmentLab.IntegrationTests.Taxation;

/// <summary>
/// Stage 7 end to end: trades in EUR → Taxation copies them from Portfolio's events → FIFO realisation in PLN at the
/// NBP rate of the business day before → Belka of the year; reversing the sale takes the tax back. Two accounts of one
/// taxpayer: lots stay within each account, gains and losses net in one PIT-38.
/// </summary>
public sealed class TaxationScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    [Fact]
    public async Task Trades_of_all_accounts_become_one_yearly_belka_assessment_and_a_reversal_recomputes_it()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsync("/api/market-data/fx-rates/ingest", null, ct)).EnsureSuccessStatusCode();
        var instruments = await client.GetFromJsonAsync<List<InstrumentRow>>("/api/market-data/instruments", ct);
        var etf = instruments!.First(instrument => instrument.Kind == "Etf").Id;

        var account = await PortfolioScenarioTests.OpenTwoSleeveAccountAsync(client, ct);
        var sleeve = account.SubPortfolios[0].Id;
        var year = DateTime.UtcNow.Year - 1;
        await TradeAsync(client, account, etf, "Buy", 10m, 100m, 1m, new DateOnly(year, 1, 14), sleeve, ct);
        await TradeAsync(client, account, etf, "Buy", 10m, 120m, 1m, new DateOnly(year, 3, 11), sleeve, ct);
        var sale = await TradeAsync(client, account, etf, "Sell", 15m, 130m, 2m, new DateOnly(year, 6, 10), sleeve, ct);

        // Another broker, a cheaper lot bought before the first account's second one: FIFO must not take it.
        var other = await PortfolioScenarioTests.OpenTwoSleeveAccountAsync(client, ct);
        var otherSleeve = other.SubPortfolios[0].Id;
        await TradeAsync(client, other, etf, "Buy", 10m, 100m, 0m, new DateOnly(year, 2, 10), otherSleeve, ct);
        await TradeAsync(client, other, etf, "Sell", 10m, 90m, 0m, new DateOnly(year, 7, 10), otherSleeve, ct);

        // The fake NBP table A has EUR at 4.30 every business day.
        // First account: proceeds 1950 × 4.30 − 2 × 4.30 = 8376.40; cost 4304.30 + half of 5164.30 = 6886.45; gain 1489.95.
        // Second account: 3870 − 4300 = −430. One PIT-38: 1059.95 → base 1060 → 19% = 201.40 → 201.
        var assessed = await EventuallyAsync(client, year, details => details.Realizations.Count == 2 && details.Summary.TaxDue == 201m, ct);
        assessed.Summary.Gain.ShouldBe(1059.95m);
        assessed.Accounts.Select(a => a.Id).ShouldBe([account.Id, other.Id], ignoreOrder: true);
        assessed.Realizations.Single(r => r.AccountId == other.Id).GainPln.ShouldBe(-430m);
        var realization = assessed.Realizations.Single(r => r.AccountId == account.Id);
        realization.ProceedsPln.ShouldBe(8376.40m);
        realization.CostPln.ShouldBe(6886.45m);
        realization.FxRate.ShouldBe(4.30m);
        realization.FxRateDate.ShouldNotBeNull().ShouldBeLessThan(new DateOnly(year, 6, 10));
        assessed.Summary.TaxBase.ShouldBe(1060m);
        realization.Attributions.ShouldHaveSingleItem().SubPortfolioId.ShouldBe(sleeve);

        // How the year grew: after the first account's gain (283) and after the second account's loss (201).
        var timeline = await client.GetFromJsonAsync<TaxTimelineDto>($"/api/taxation/years/{year}/timeline", ct);
        timeline!.Points.Select(point => point.TaxDue).ShouldBe([283m, 201m]);

        (await client.PutAsJsonAsync("/api/taxation/settings", new TaxSettingsDto(0.02m), ct)).EnsureSuccessStatusCode();
        (await client.GetFromJsonAsync<TaxSettingsDto>("/api/taxation/settings", ct))!.AlertShare.ShouldBe(0.02m);
        (await client.PutAsJsonAsync("/api/taxation/settings", new TaxSettingsDto(1.5m), ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.PostAsJsonAsync($"/api/portfolio/accounts/{account.Id}/transactions/{sale}/reverse", new { reason = "помилка" }, ct)).EnsureSuccessStatusCode();
        var reversed = await EventuallyAsync(client, year, details => details.Realizations.Count == 1, ct);
        reversed.Summary.TaxDue.ShouldBe(0m);
        reversed.Summary.LossToCarry.ShouldBe(430m);

        var stranger = await factory.CreateSignedInClientAsync(ct);
        (await stranger.GetFromJsonAsync<TaxOverviewDto>("/api/taxation/years", ct))!.Years.ShouldBeEmpty();
        (await stranger.GetAsync($"/api/taxation/years/{year}", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.GetFromJsonAsync<TaxSettingsDto>("/api/taxation/settings", ct))!.AlertShare.ShouldBeNull();
    }

    private static async Task<Guid> TradeAsync(
        HttpClient client, AccountSetupDto account, Guid instrumentId, string side, decimal quantity, decimal price, decimal fee, DateOnly on, Guid sleeve, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"/api/portfolio/accounts/{account.Id}/trades", new
        {
            instrumentId, side, quantity, price, fee, executedOn = on, allocations = new[] { new { subPortfolioId = sleeve, quantity } },
        }, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<RecordedRow>(ct))!.TransactionId;
    }

    private static async Task<TaxYearDetailsDto> EventuallyAsync(HttpClient client, int year, Func<TaxYearDetailsDto, bool> condition, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (true)
        {
            var response = await client.GetAsync($"/api/taxation/years/{year}", ct);
            if (response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TaxYearDetailsDto>(ct) is { } details && condition(details))
                return details;
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Tax year {year} did not reach the expected state: {await response.Content.ReadAsStringAsync(ct)}");
            await Task.Delay(300, ct);
        }
    }

    private sealed record InstrumentRow(Guid Id, string Kind);

    private sealed record RecordedRow(Guid TransactionId);
}
