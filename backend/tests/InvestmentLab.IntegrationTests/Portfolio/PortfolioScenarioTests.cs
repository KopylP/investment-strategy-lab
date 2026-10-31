using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Transactions.Application.ImportPositions;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.IntegrationTests.Portfolio;

public sealed class PortfolioScenarioTests(PortfolioApiFactory factory) : IClassFixture<PortfolioApiFactory>
{
    [Fact]
    public async Task Api_needs_a_signed_in_user()
    {
        var response = await factory.CreateClient().GetAsync("/api/portfolio/accounts", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_account_is_visible_only_to_its_owner()
    {
        var ct = TestContext.Current.CancellationToken;
        var owner = await factory.CreateSignedInClientAsync(ct);
        var account = await OpenTwoSleeveAccountAsync(owner, ct);

        account.SubPortfolios.Select(sub => sub.TargetShare).ShouldBe([0.7m, 0.3m]);
        (await owner.GetFromJsonAsync<List<AccountSetupDto>>("/api/portfolio/accounts", ct))!.ShouldHaveSingleItem().Id.ShouldBe(account.Id);

        var stranger = await factory.CreateSignedInClientAsync(ct);
        (await stranger.GetAsync($"/api/portfolio/accounts/{account.Id}", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await stranger.GetFromJsonAsync<List<AccountSetupDto>>("/api/portfolio/accounts", ct))!.ShouldBeEmpty();
    }

    [Fact]
    public async Task Imported_positions_json_gives_the_same_positions_and_cash_and_a_reversal_restores_them()
    {
        var ct = TestContext.Current.CancellationToken;
        var client = await factory.CreateSignedInClientAsync(ct);
        var account = await OpenTwoSleeveAccountAsync(client, ct);
        var (eu, us) = (account.SubPortfolios[0].Id, account.SubPortfolios[1].Id);
        var file = await File.ReadAllTextAsync(PositionsJsonPath(), ct);
        using var expected = JsonDocument.Parse(file);

        var import = await PostAsync<ImportReport>(client,
            $"/api/portfolio/accounts/{account.Id}/sub-portfolios/{eu}/import-positions?asOf=2026-05-04", new StringContent(file, System.Text.Encoding.UTF8, "application/json"), ct);
        var positionCount = expected.RootElement.GetProperty("positions").GetArrayLength();
        import.Recorded.ShouldBe(positionCount + 1);
        import.Warnings.ShouldBeEmpty();

        var instruments = await client.GetFromJsonAsync<List<InstrumentDto>>("/api/market-data/instruments", ct);
        var tickerOf = instruments!.SelectMany(i => i.ProviderSymbols.Where(s => s.StartsWith("yfinance:")).Select(s => (i.Id, Ticker: s["yfinance:".Length..])))
            .ToDictionary(pair => pair.Id, pair => pair.Ticker);

        var imported = await HoldingsAsync(client, account.Id, ct);
        var euHoldings = imported.SubPortfolios.Single(sub => sub.SubPortfolioId == eu);
        euHoldings.Cash.ShouldBe(expected.RootElement.GetProperty("cash").GetDecimal());
        euHoldings.Positions.Select(p => (tickerOf[p.InstrumentId], p.Quantity, p.AverageCost)).OrderBy(p => p.Item1).ShouldBe(
            [.. expected.RootElement.GetProperty("positions").EnumerateArray()
                .Select(p => (p.GetProperty("ticker").GetString()!, p.GetProperty("shares").GetDecimal(), p.GetProperty("entry_price").GetDecimal()))
                .OrderBy(p => p.Item1)]);
        imported.SubPortfolios.Single(sub => sub.SubPortfolioId == us).ShouldSatisfyAllConditions(
            sub => sub.Cash.ShouldBe(0m), sub => sub.Positions.ShouldBeEmpty());

        var again = await PostAsync<ImportReport>(client,
            $"/api/portfolio/accounts/{account.Id}/sub-portfolios/{eu}/import-positions?asOf=2026-05-04", new StringContent(file, System.Text.Encoding.UTF8, "application/json"), ct);
        (again.Recorded, again.Skipped).ShouldBe((0, positionCount + 1));

        var position = euHoldings.Positions[0];
        var sale = await PostAsync<RecordedTransaction>(client, $"/api/portfolio/accounts/{account.Id}/trades",
            JsonContent.Create(new { instrumentId = position.InstrumentId, side = "Sell", quantity = 1m, price = 20m, fee = 0.5m, executedOn = "2026-10-20" }), ct);
        var afterSale = await HoldingsAsync(client, account.Id, ct);
        afterSale.SubPortfolios.Single(sub => sub.SubPortfolioId == eu).Cash.ShouldBe(euHoldings.Cash + 19.5m);

        var oversell = await client.PostAsJsonAsync($"/api/portfolio/accounts/{account.Id}/trades",
            new { instrumentId = position.InstrumentId, side = "Sell", quantity = position.Quantity, price = 20m, fee = 0m, executedOn = "2026-10-21" }, ct);
        oversell.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        await PostAsync<RecordedTransaction>(client, $"/api/portfolio/accounts/{account.Id}/transactions/{sale.TransactionId}/reverse",
            JsonContent.Create(new { reason = "Sold in the wrong account" }), ct);
        var restored = await HoldingsAsync(client, account.Id, ct);
        Snapshot(restored).ShouldBe(Snapshot(imported));
    }

    private static async Task<AccountHoldingsDto> HoldingsAsync(HttpClient client, Guid accountId, CancellationToken ct) =>
        (await client.GetFromJsonAsync<AccountHoldingsDto>($"/api/portfolio/accounts/{accountId}/holdings", ct))!;

    private static IEnumerable<string> Snapshot(AccountHoldingsDto holdings) =>
        holdings.SubPortfolios.SelectMany(sub => sub.Positions.Select(p => $"{sub.SubPortfolioId}:{p.InstrumentId}:{p.Quantity}:{p.CostBasis}:{p.Lots.Count}")
            .Prepend($"{sub.SubPortfolioId}:cash:{sub.Cash}"));

    private static async Task<T> PostAsync<T>(HttpClient client, string url, HttpContent content, CancellationToken ct)
    {
        var response = await client.PostAsync(url, content, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));

        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    internal static string PositionsJsonPath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "strategy-engine", "src", "finance_decisions", "strategies", "etf_momentum", "positions.json");
            if (File.Exists(path))
                return path;
        }

        throw new FileNotFoundException("positions.json of the Python flow was not found above the test directory.");
    }

    internal static async Task<AccountSetupDto> OpenTwoSleeveAccountAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.PostAsJsonAsync("/api/portfolio/accounts", new
        {
            name = "XTB EUR",
            broker = "XTB",
            baseCurrency = "EUR",
            subPortfolios = new object[]
            {
                new { name = "EU momentum", targetSharePercent = 70m },
                new { name = "US momentum", targetSharePercent = 30m, maxPositionWeightPercent = 40m },
            },
        }, ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(ct));

        return (await response.Content.ReadFromJsonAsync<AccountSetupDto>(ct))!;
    }
}
