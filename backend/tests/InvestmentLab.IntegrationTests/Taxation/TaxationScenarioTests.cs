using System.Net;
using System.Net.Http.Json;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.IntegrationTests.Portfolio;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Application;
using InvestmentLab.Taxation.Declarations.Application;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.IntegrationTests.Taxation;

public sealed class TaxationScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    [Fact]
    public async Task Trades_become_a_yearly_belka_assessment_and_a_reversal_recomputes_it()
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

        var assessed = await EventuallyAsync(client, year, details => details.Summary.TaxDue == 283m, ct);
        var realization = assessed.Realizations.ShouldHaveSingleItem();
        realization.ProceedsPln.ShouldBe(8385.00m);
        realization.CostPln.ShouldBe(6895.05m);
        realization.FxRate.ShouldBe(4.30m);
        realization.FxRateDate.ShouldNotBeNull().ShouldBeLessThan(new DateOnly(year, 6, 10));
        assessed.Summary.TaxBase.ShouldBe(1490m);
        realization.Attributions.ShouldHaveSingleItem().SubPortfolioId.ShouldBe(sleeve);

        (await client.GetFromJsonAsync<List<DeclarationFormDto>>($"/api/taxation/declarations/{year}", ct))!.ShouldHaveSingleItem().Code.ShouldBe("PIT-38");
        var pit38 = (await client.GetFromJsonAsync<DeclarationRow>($"/api/taxation/declarations/{year}/pit-38", ct))!.Declaration;
        var fields = pit38.Sections.SelectMany(section => section.Fields).ToDictionary(field => field.Position, field => field.Amount);
        (fields[20], fields[21], fields[22]).ShouldBe((8385.00m, 6895.05m, 0m));
        (fields[31], fields[35], fields[51]).ShouldBe((1490m, 283m, 283m));
        (await client.GetAsync($"/api/taxation/declarations/{year - 5}/pit-38", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        foreach (var kind in new[] { "summary", "form" })
        {
            var pdf = await client.GetAsync($"/api/taxation/declarations/{year}/pit-38/pdf?kind={kind}", ct);
            pdf.StatusCode.ShouldBe(HttpStatusCode.OK);
            pdf.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
            (await pdf.Content.ReadAsByteArrayAsync(ct)).Take(4).ShouldBe("%PDF"u8.ToArray());
        }

        var other = await PortfolioScenarioTests.OpenTwoSleeveAccountAsync(client, ct);
        await TradeAsync(client, other, etf, "Buy", 10m, 100m, 0m, new DateOnly(year, 2, 10), other.SubPortfolios[0].Id, ct);
        await TradeAsync(client, other, etf, "Sell", 10m, 90m, 0m, new DateOnly(year, 7, 10), other.SubPortfolios[0].Id, ct);
        var progress = (await client.GetFromJsonAsync<TaxProgressDto>($"/api/taxation/progress/{year}", ct))!;
        progress.Points.Select(point => (point.Date, point.TaxDue)).ShouldBe([(new DateOnly(year, 6, 10), 283m), (new DateOnly(year, 7, 10), 201m)]);
        (progress.TaxDue, progress.Gain, progress.TaxedAccounts).ShouldBe((201m, 1059.95m, 2));
        var both = await EventuallyAsync(client, year, details => details.Realizations.Count == 2, ct);
        both.Realizations.Single(r => r.AccountId == account.Id).CostPln.ShouldBe(6895.05m);
        both.Accounts.Select(a => a.Id).ShouldBe([account.Id, other.Id], ignoreOrder: true);
        var netted = (await client.GetFromJsonAsync<DeclarationRow>($"/api/taxation/declarations/{year}/pit-38", ct))!.Declaration;
        netted.TaxDue.ShouldBe(201m);

        progress.AlertShare.ShouldBe(0.01m);

        (await client.PostAsJsonAsync($"/api/portfolio/accounts/{account.Id}/transactions/{sale}/reverse", new { reason = "помилка" }, ct)).EnsureSuccessStatusCode();

        var reversed = await EventuallyAsync(client, year, details => details.Realizations.All(r => r.AccountId == other.Id), ct);
        reversed.Summary.TaxDue.ShouldBe(0m);
        reversed.Summary.LossToCarry.ShouldBe(430m);

        var stranger = await factory.CreateSignedInClientAsync(ct);
        (await stranger.GetFromJsonAsync<TaxOverviewDto>("/api/taxation/years", ct))!.Years.ShouldBeEmpty();
        (await stranger.GetAsync($"/api/taxation/years/{year}", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.GetFromJsonAsync<TaxProgressDto>($"/api/taxation/progress/{year}", ct))!.ShouldSatisfyAllConditions(
            mine => mine.Points.ShouldBeEmpty(), mine => mine.TaxDue.ShouldBe(0m));
    }

    private static async Task<Guid> TradeAsync(
        HttpClient client, AccountSetupDto account, Guid instrumentId, string side, decimal quantity, decimal price, decimal fee, DateOnly on, Guid sleeve, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync($"/api/portfolio/accounts/{account.Id}/trades", new
        {
            instrumentId,
            side,
            quantity,
            price,
            fee,
            executedOn = on,
            allocations = new[] { new { subPortfolioId = sleeve, quantity } },
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

    private sealed record DeclarationRow(TaxDeclaration Declaration);

    private sealed record RecordedRow(Guid TransactionId);
}
