using System.Net;
using System.Net.Http.Json;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.IntegrationTests.Portfolio;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.IntegrationTests.Imports;

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
        sxr8.InstrumentId.ShouldNotBeNull();
        preview.Mappings.Single(mapping => mapping.Ticker == "AAPL").InstrumentName.ShouldBe("Apple");
        preview.Counts.NeedsMapping.ShouldBe(3);

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

        configured.Rows.Single(row => row.Ticker == "AAPL").AmountInAccountCurrency.ShouldBe(195m);

        var confirmed = await SendAsync(client, HttpMethod.Post, $"{url}/{preview.Id}/confirm", new { }, ct);
        confirmed.Status.ShouldBe("Confirmed");
        confirmed.Rows.Where(row => row.WillRecord && row.Ticker != "SFGYY").ShouldAllBe(row => row.Result == "@statement.row.recorded");

        confirmed.Rows.Single(row => row.Ticker == "SFGYY").Result.ShouldBe("@statement.row.recorded_as_income");

        var holdings = await client.GetFromJsonAsync<AccountHoldingsDto>($"/api/portfolio/accounts/{account.Id}/holdings", ct);
        var sxr8Position = holdings!.SubPortfolios.Single(sub => sub.SubPortfolioId == eu).Positions.Single();
        sxr8Position.Quantity.ShouldBe(2m);
        sxr8Position.CostBasis.ShouldBe(201m);

        var apple = holdings.SubPortfolios.Single(sub => sub.SubPortfolioId == us).Positions.ShouldHaveSingleItem();
        apple.AverageCost.ShouldBe(195m);
        holdings.SubPortfolios.Single(sub => sub.SubPortfolioId == eu).Cash.ShouldBe(1000m - 201m + 1.50m);

        var usSleeve = holdings.SubPortfolios.Single(sub => sub.SubPortfolioId == us);
        usSleeve.Cash.ShouldBe(0m);
        usSleeve.ForeignCash.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(new CashBalanceDto("USD", -215m + 2m));

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

    [Fact]
    public async Task Unknown_tickers_are_looked_up_on_yahoo_rates_are_loaded_and_a_single_sleeve_is_chosen_so_the_import_only_needs_confirming()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);

        var opened = await client.PostAsJsonAsync("/api/portfolio/accounts", new
        {
            name = "Revolut",
            broker = "Revolut",
            baseCurrency = "EUR",
            subPortfolios = new object[] { new { name = "Акції", targetSharePercent = 100m } },
        }, ct);
        var account = (await opened.Content.ReadFromJsonAsync<AccountSetupDto>(ct))!;
        const string statement = """
            Date,Ticker,Type,Quantity,Price per share,Total Amount,Currency,FX Rate
            2026-06-01T09:00:00.100Z,,CASH TOP-UP,,,EUR 1000,EUR,0.2340
            2026-06-02T10:15:30.200Z,RHM,BUY - MARKET,1,EUR 100,EUR 100,EUR,0.2340
            2026-06-03T15:30:00.300Z,ZZZZ,BUY - MARKET,1,USD 10,USD 10,USD,0.2564
            """;

        var preview = await SendAsync(client, HttpMethod.Post, $"/api/portfolio/accounts/{account.Id}/statement-imports",
            new { fileName = "revolut.csv", content = statement }, ct);

        var rheinmetall = preview.Mappings.Single(mapping => mapping.Ticker == "RHM");
        (rheinmetall.InstrumentId, rheinmetall.RegisterAs, rheinmetall.InstrumentName).ShouldBe((null, "RHM.DE", "Rheinmetall AG"));
        var unknown = preview.Mappings.Single(mapping => mapping.Ticker == "ZZZZ");
        (unknown.RegisterAs, unknown.InstrumentName).ShouldBe(("ZZZZ", null));
        preview.Mappings.ShouldAllBe(mapping => mapping.SubPortfolioId == account.SubPortfolios[0].Id);
        preview.Counts.NeedsMapping.ShouldBe(0);

        var confirmed = await SendAsync(client, HttpMethod.Post, $"/api/portfolio/accounts/{account.Id}/statement-imports/{preview.Id}/confirm", new { }, ct);
        confirmed.Mappings.Single(mapping => mapping.Ticker == "RHM").InstrumentName.ShouldBe("Rheinmetall AG");
    }

    private static async Task<StatementImportDto> SendAsync(HttpClient client, HttpMethod method, string url, object body, CancellationToken ct)
    {
        var response = await client.SendAsync(new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) }, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));

        return (await response.Content.ReadFromJsonAsync<StatementImportDto>(ct))!;
    }
}
