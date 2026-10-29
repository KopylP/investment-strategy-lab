extern alias api;

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace InvestmentLab.IntegrationTests.Identity;

/// <summary>
/// The API as the Home Assistant add-on runs it (ingress on 8098). The test server has no sockets, so a request says
/// where it comes from in <see cref="PeerHeader"/> ("172.30.32.2:8098" is the Supervisor on the ingress port).
/// </summary>
public class HomeAssistantApiFactory : WebApplicationFactory<api::Program>, IAsyncLifetime
{
    public const string PeerHeader = "X-Test-Peer";
    public const string Supervisor = "172.30.32.2:8098";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:investmentlab", _postgres.GetConnectionString())
            .UseSetting("Scheduling:RunAtStartup", "false")
            .UseSetting("HomeAssistant:IngressPort", "8098")
            .ConfigureTestServices(services => services.AddTransient<IStartupFilter, PeerFromHeader>());

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class PeerFromHeader : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers[PeerHeader].ToString() is { Length: > 0 } peer && IPEndPoint.TryParse(peer, out var endpoint))
                {
                    context.Connection.RemoteIpAddress = endpoint.Address;
                    context.Connection.LocalPort = endpoint.Port;
                }
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
