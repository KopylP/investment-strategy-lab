using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.StrategyBuilder.Drafts.Application.ListInstruments;
using InvestmentLab.StrategyBuilder.Funds.Application.AddFunds;
using InvestmentLab.StrategyBuilder.Funds.Application.GetFundSearchOptions;
using InvestmentLab.StrategyBuilder.Funds.Application.SearchFunds;

namespace InvestmentLab.IntegrationTests.StrategyBuilder;

public sealed class FundSearchScenarioTests(MarketDataApiFactory factory) : IClassFixture<MarketDataApiFactory>
{
    [Fact]
    public async Task Found_funds_register_independently_reuse_known_symbols_and_join_the_directory()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await factory.CreateSignedInClientAsync(cancellationToken);
        var options = await client.GetFromJsonAsync<FundSearchOptionsDto>("/api/strategy-builder/funds/options", cancellationToken);
        options!.Exchanges.ShouldHaveSingleItem().Label.ShouldBe("@market_data.exchange.amsterdam");

        using var invalid = await client.PostAsJsonAsync("/api/strategy-builder/funds/search", new { }, cancellationToken);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var placeError = await invalid.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        placeError.GetProperty("code").GetString().ShouldBe("builder.funds.place_required");

        var found = await SearchAsync(client, cancellationToken);
        found.Count.ShouldBe(3);
        found[0].InstrumentId.ShouldNotBeNull();
        found[1].InstrumentId.ShouldBeNull();
        var additions = found.Select(fund => new FundToAddDto(fund.Symbol, fund.Name, fund.Currency, fund.InstrumentId)).ToList();
        using var registered = await client.PostAsJsonAsync("/api/strategy-builder/funds", new AddFundsCommand(additions, []), cancellationToken);
        registered.EnsureSuccessStatusCode();
        var added = (await registered.Content.ReadFromJsonAsync<AddedFundsDto>(cancellationToken))!;
        added.InstrumentIds.Count.ShouldBe(2);
        added.Failed.ShouldHaveSingleItem().Symbol.ShouldBe("NO_CURRENCY");

        var repeated = await SearchAsync(client, cancellationToken);
        repeated[1].InstrumentId.ShouldNotBeNull();
        repeated[0].InstrumentId.ShouldBe(found[0].InstrumentId);
        using var again = await client.PostAsJsonAsync("/api/strategy-builder/funds", new AddFundsCommand(additions.Take(2).ToList(), []), cancellationToken);
        again.EnsureSuccessStatusCode();
        var reused = (await again.Content.ReadFromJsonAsync<AddedFundsDto>(cancellationToken))!;
        reused.InstrumentIds.ShouldBe(added.InstrumentIds);

        var directory = await client.GetFromJsonAsync<List<InstrumentChoiceDto>>("/api/strategy-builder/instruments", cancellationToken);
        directory!.Count(instrument => instrument.Id == repeated[1].InstrumentId).ShouldBe(1);
        directory!.Single(instrument => instrument.Id == repeated[1].InstrumentId).Name.ShouldBe("Discovered fund");

        var fullSelection = Enumerable.Range(0, 60).Select(_ => Guid.NewGuid()).ToList();
        using var tooMany = await client.PostAsJsonAsync("/api/strategy-builder/funds", new AddFundsCommand(additions.Take(1).ToList(), fullSelection), cancellationToken);
        tooMany.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        factory.Funds.Unavailable = true;
        try
        {
            using var unavailable = await client.PostAsJsonAsync("/api/strategy-builder/funds/search", new { exchangeId = "place" }, cancellationToken);
            unavailable.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            var error = await unavailable.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            error.GetProperty("code").GetString().ShouldBe("market_data.fund_screen.unavailable");
        }
        finally
        {
            factory.Funds.Unavailable = false;
        }
    }

    private static async Task<List<FoundFundDto>> SearchAsync(HttpClient client, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync("/api/strategy-builder/funds/search", new { exchangeId = "place" }, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<List<FoundFundDto>>(cancellationToken))!;
    }
}
