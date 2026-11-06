using InvestmentLab.Identity.Users.Domain;
using InvestmentLab.IntegrationTests.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.IntegrationTests.Mcp;

public sealed class McpApiFactory : HomeAssistantApiFactory
{
    public string? StrategyEngineUrl { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        if (StrategyEngineUrl is not null)
            builder.UseSetting("Strategies:StrategyEngineUrl", StrategyEngineUrl);

        builder.UseSetting("Mcp:Enabled", "true")
            .UseSetting("Identity:AccessTokens:Issuer", "HomeAssistant")
            .UseSetting("HomeAssistant:IngressOnly", "true")
            .ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccessTokenValidator>();
                services.AddSingleton<IAccessTokenValidator, FakeHomeAssistantTokens>();
            });
    }
}
