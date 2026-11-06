using System.Net.Http.Json;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.IntegrationTests.MarketData;

public sealed class QuoteArchiveScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    [Fact]
    public async Task A_fund_whose_quotes_were_never_ingested_is_priced_from_the_archive_in_a_recent_window()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        var days = FakeQuoteProvider.TradingDays();
        var recentWindow = days.Skip(5).ToList();

        var instruments = await client.GetFromJsonAsync<List<InstrumentDto>>("/api/market-data/instruments", ct);
        var fund = instruments!.Single(instrument => instrument.ProviderSymbols.Contains($"yfinance:{FakeQuoteProvider.Symbol}"));

        using var scope = factory.Services.CreateScope();
        var quotes = scope.ServiceProvider.GetRequiredService<IQuoteHistory>();
        var history = await quotes.GetQuoteHistoryWithArchive([fund.Id], recentWindow[0], recentWindow[^1], ct);

        var dates = history.ShouldHaveSingleItem().Points.Select(point => point.Date);
        dates.ShouldBe(recentWindow);
    }
}
