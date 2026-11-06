using InvestmentLab.Identity.Users.Domain;
using InvestmentLab.IntegrationTests.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.IntegrationTests.Mcp;

public sealed class McpApiFactory : HomeAssistantApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
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
