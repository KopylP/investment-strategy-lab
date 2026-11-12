using System.Net;
using System.Net.Http.Json;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;

namespace InvestmentLab.IntegrationTests.MarketData;

public sealed class MarketDataScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    [Fact]
    public async Task Ingested_quotes_can_be_corrected_and_the_correction_survives_the_next_ingest()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        var days = FakeQuoteProvider.TradingDays();

        var instruments = await client.GetFromJsonAsync<List<InstrumentDto>>("/api/market-data/instruments", ct);
        var sp500 = instruments!.Single(instrument => instrument.ProviderSymbols.Contains($"yfinance:{FakeQuoteProvider.Symbol}"));

        var firstIngest = await PostForAsync<IngestReport>(client, "/api/market-data/quotes/ingest", ct);
        firstIngest.QuotesAdded.ShouldBe(15);
        firstIngest.MissingSymbols.ShouldNotContain(FakeQuoteProvider.Symbol);

        var history = await client.GetFromJsonAsync<QuoteSeriesDto>(
            $"/api/market-data/quotes/{sp500.Id}?from={days[0]:yyyy-MM-dd}&to={days[^1]:yyyy-MM-dd}", ct);
        history!.Points.Select(point => point.Close).ShouldBe([.. Enumerable.Range(100, 15).Select(close => (decimal)close)]);
        history.Points.ShouldAllBe(point => point.Open == point.Close - 1m && point.High == point.Close + 2m && point.Low == point.Close - 2m);

        var correction = await client.PostAsJsonAsync($"/api/market-data/quotes/{sp500.Id}/corrections",
            new { tradingDate = days[^1], close = 120m, reason = "Broker statement shows 120" }, ct);
        correction.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await WaitUntilAsync(() => factory.CorrectedQuotes.Any(e => e.InstrumentId == sp500.Id && e.TradingDate == days[^1]), ct);

        factory.Quotes.NewestCloseOverride = 999m;
        var secondIngest = await PostForAsync<IngestReport>(client, "/api/market-data/quotes/ingest", ct);
        secondIngest.QuotesAdded.ShouldBe(0);

        var latest = await client.GetFromJsonAsync<List<LatestPriceDto>>($"/api/market-data/quotes/latest?instrumentIds={sp500.Id}", ct);
        latest!.ShouldHaveSingleItem().Close.ShouldBe(120m);
        latest[0].IsStale.ShouldBeFalse();
    }

    [Fact]
    public async Task Fx_rate_for_a_transaction_comes_from_the_previous_business_day()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        await client.PostAsync("/api/market-data/fx-rates/ingest", null, ct);

        var monday = FakeQuoteProvider.TradingDays().Last(day => day.DayOfWeek == DayOfWeek.Monday);
        var rate = await client.GetFromJsonAsync<FxRateDto>($"/api/market-data/fx-rates/EUR/PLN?transactionDate={monday:yyyy-MM-dd}", ct);

        rate!.EffectiveDate.ShouldBe(monday.AddDays(-3));
        rate.Rate.ShouldBe(4.30m);
        rate.TableNumber.ShouldNotBeNull();
    }

    private static async Task<T> PostForAsync<T>(HttpClient client, string url, CancellationToken ct)
    {
        var response = await client.PostAsync(url, null, ct);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
            await Task.Delay(100, ct);

        condition().ShouldBeTrue("the integration event did not arrive from the outbox");
    }
}
