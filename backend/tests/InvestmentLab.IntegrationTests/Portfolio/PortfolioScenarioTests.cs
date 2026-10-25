using System.Net;
using System.Net.Http.Json;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.IntegrationTests.Portfolio;

/// <summary>Stage 3 end to end: sign-in → account with two sub-portfolios → import positions.json → reversal.</summary>
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
