using System.Net;

namespace InvestmentLab.IntegrationTests;

public sealed class AppHostStartupTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Api_is_healthy_with_database()
    {
        using var client = fixture.CreateApiClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Api_publishes_open_api_document()
    {
        using var client = fixture.CreateApiClient();

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
