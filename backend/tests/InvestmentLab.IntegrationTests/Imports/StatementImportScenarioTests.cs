using System.Net;
using System.Net.Http.Json;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.IntegrationTests.Portfolio;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Transactions.Application.ImportStatement;

namespace InvestmentLab.IntegrationTests.Imports;

/// <summary>
/// Stage 10 end to end: a Revolut statement (synthetic) → suggested mapping, a new US stock registered → preview →
/// confirmed rows in the journal (USD converted at the NBP cross rate) → the same file again changes nothing →
/// undoing the import takes the batch back.
/// </summary>
public sealed class StatementImportScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    private const string Statement = """
        Date,Ticker,Type,Quantity,Price per share,Total Amount,Currency,FX Rate
        2026-06-01T09:00:00.100Z,,CASH TOP-UP,,,EUR 1000,EUR,0.2340
        2026-06-02T10:15:30.200Z,SXR8,BUY - MARKET,2,EUR 100,EUR 201,EUR,0.2340
        2026-06-03T15:30:00.300Z,AAPL,BUY - MARKET,1,USD 215,USD 215,USD,0.2564
        2026-06-10T12:00:00.400Z,SXR8,DIVIDEND,,,EUR 1.50,EUR,0.2341
        2026-06-15T15:00:00.500Z,SFGYY,SELL - MARKET,0.5,USD 4,USD 2,USD,0.2564
        2026-06-21T15:00:00.700Z,SXR8,STOCK SPLIT,2,,,EUR,0.2343
        """;

    [Fact]
    public async Task A_statement_is_previewed_imported_once_and_can_be_undone()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsync("/api/market-data/fx-rates/ingest", null, ct)).EnsureSuccessStatusCode();
        var account = await PortfolioScenarioTests.OpenTwoSleeveAccountAsync(client, ct);
        var (eu, us) = (account.SubPortfolios[0].Id, account.SubPortfolios[1].Id);
        var url = $"/api/portfolio/accounts/{account.Id}/statement-imports";

        var preview = await SendAsync(client, HttpMethod.Post, url, new { fileName = "revolut.csv", content = Statement }, ct);
        preview.Rows.Count.ShouldBe(6);
        var sxr8 = preview.Mappings.Single(mapping => mapping.Ticker == "SXR8");
        sxr8.InstrumentId.ShouldNotBeNull();                                  // found in the catalog as SXR8.DE
        preview.Mappings.Single(mapping => mapping.Ticker == "AAPL").RegisterAs.ShouldBe("AAPL");
        preview.Counts.NeedsMapping.ShouldBe(3);                              // no strategy says which sleeve for the buys (the dividend falls back to the cash sleeve)

        var configured = await SendAsync(client, HttpMethod.Put, $"{url}/{preview.Id}", new
        {
            mode = "Merge",
            cashSubPortfolioId = eu,
            mappings = new object[]
            {
                new { ticker = "SXR8", instrumentId = sxr8.InstrumentId, subPortfolioId = eu },
                new { ticker = "AAPL", registerAs = "AAPL", subPortfolioId = us },
                new { ticker = "SFGYY", registerAs = "SFGYY", subPortfolioId = us },
            },
        }, ct);
        configured.Counts.ShouldSatisfyAllConditions(
            counts => counts.New.ShouldBe(5),
            counts => counts.Unsupported.ShouldBe(1),
            counts => counts.ToRecord.ShouldBe(5));
        // USD 215 at the fake NBP rates (USD 3.90, EUR 4.30 PLN) → 195.00 EUR.
        configured.Rows.Single(row => row.Ticker == "AAPL").AmountInAccountCurrency.ShouldBe(195m);

        var confirmed = await SendAsync(client, HttpMethod.Post, $"{url}/{preview.Id}/confirm", new { }, ct);
        confirmed.Status.ShouldBe("Confirmed");
        confirmed.Rows.Where(row => row.WillRecord && row.Ticker != "SFGYY").ShouldAllBe(row => row.Result == "записано");
        // Sold without a buy (a spin-off): recorded as income, not refused.
        confirmed.Rows.Single(row => row.Ticker == "SFGYY").Result.ShouldStartWith("записано як дохід");

        var holdings = await client.GetFromJsonAsync<AccountHoldingsDto>($"/api/portfolio/accounts/{account.Id}/holdings", ct);
        var sxr8Position = holdings!.SubPortfolios.Single(sub => sub.SubPortfolioId == eu).Positions.Single();
        sxr8Position.Quantity.ShouldBe(2m);
        sxr8Position.CostBasis.ShouldBe(201m);                                // the 1 € hidden in the total is the fee

        var apple = holdings.SubPortfolios.Single(sub => sub.SubPortfolioId == us).Positions.ShouldHaveSingleItem();
        apple.AverageCost.ShouldBe(195m);
        holdings.SubPortfolios.Single(sub => sub.SubPortfolioId == eu).Cash.ShouldBe(1000m - 201m + 1.50m);
        // The AAPL buy was settled in dollars: the USD balance pays, the EUR cash of its sleeve does not move.
        var usSleeve = holdings.SubPortfolios.Single(sub => sub.SubPortfolioId == us);
        usSleeve.Cash.ShouldBe(0m);
        usSleeve.ForeignCash.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(new CashBalanceDto("USD", -215m + 2m));

        // The same statement again: everything is already there.
        var again = await SendAsync(client, HttpMethod.Post, url, new { fileName = "revolut.csv", content = Statement }, ct);
        again = await SendAsync(client, HttpMethod.Put, $"{url}/{again.Id}", new
        {
            mode = "Merge",
            cashSubPortfolioId = eu,
            mappings = again.Mappings.Select(mapping => new { ticker = mapping.Ticker, instrumentId = mapping.InstrumentId, subPortfolioId = mapping.Ticker is "AAPL" or "SFGYY" ? us : eu }),
        }, ct);
        again.Counts.ShouldSatisfyAllConditions(counts => counts.AlreadyImported.ShouldBe(5), counts => counts.ToRecord.ShouldBe(0));

        var undone = await SendAsync(client, HttpMethod.Post, $"{url}/{preview.Id}/undo", new { }, ct);
        undone.Status.ShouldBe("Undone");
        var after = await client.GetFromJsonAsync<AccountHoldingsDto>($"/api/portfolio/accounts/{account.Id}/holdings", ct);
        after!.SubPortfolios.ShouldAllBe(sub => sub.Positions.Count == 0 && sub.Cash == 0m);
    }

    private static async Task<StatementImportDto> SendAsync(HttpClient client, HttpMethod method, string url, object body, CancellationToken ct)
    {
        var response = await client.SendAsync(new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) }, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));
        return (await response.Content.ReadFromJsonAsync<StatementImportDto>(ct))!;
    }
}
