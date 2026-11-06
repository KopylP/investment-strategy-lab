using System.Net;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.Identity;
using InvestmentLab.Identity.Users.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace InvestmentLab.Api.Hosting;

public static class McpAccess
{
    public const string Path = "/api/investlab/mcp";
    public const string EnabledSetting = "Mcp:Enabled";
    public const string CoreProxySetting = "HomeAssistant:CoreProxyAddress";

    private const string ResourceMetadataPrefix = "/.well-known/oauth-protected-resource";
    private const string ResourceName = "Investment Lab";

    private static readonly IPAddress DefaultCoreProxy = IPAddress.Parse("172.30.32.1");

    public static void AddMcpAccess(this WebApplicationBuilder builder, IEnumerable<IModule> modules)
    {
        if (!IsEnabled(builder.Configuration))
            return;

        RequireAccessTokenIssuer(builder.Configuration);

        AddServer(builder.Services, modules);
        AddResourceAuthentication(builder.Services);
        TrustHomeAssistantCoreForwarding(builder);
    }

    public static void UseMcpForwarding(this WebApplication app)
    {
        if (IsEnabled(app.Configuration))
            app.UseForwardedHeaders();
    }

    public static void MapMcpAccess(this WebApplication app)
    {
        if (!IsEnabled(app.Configuration))
            return;

        var signedInByAccessToken = new AuthorizationPolicyBuilder(IdentityModule.AccessTokenScheme)
            .RequireAuthenticatedUser()
            .Build();

        app.MapMcp(Path).RequireAuthorization(signedInByAccessToken);
    }

    public static bool IsMcpRequest(HttpContext context)
    {
        if (!IsEnabled(context.RequestServices.GetRequiredService<IConfiguration>()))
            return false;

        var path = context.Request.Path;

        return path.StartsWithSegments(Path) || path.StartsWithSegments(ResourceMetadataPrefix + Path);
    }

    private static bool IsEnabled(IConfiguration configuration) => configuration.GetValue<bool>(EnabledSetting);

    private static void RequireAccessTokenIssuer(IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(configuration[IdentityModule.AccessTokenIssuerSetting]))
            throw new InvalidOperationException(
                $"{EnabledSetting} is on, but {IdentityModule.AccessTokenIssuerSetting} names no issuer of access tokens.");
    }

    private static void AddServer(IServiceCollection services, IEnumerable<IModule> modules)
    {
        var server = services.AddMcpServer(options => options.ServerInstructions = McpInstructions.Text)
            .WithHttpTransport(http => http.Stateless = true)
            .AddAuthorizationFilters()
            .WithLocalizedReplies();

        foreach (var module in modules)
            server.WithToolsFromAssembly(module.GetType().Assembly, McpJson.Options);
    }

    private static void AddResourceAuthentication(IServiceCollection services)
    {
        services.AddAuthentication().AddMcp(options =>
        {
            options.ResourceMetadata = new ProtectedResourceMetadata
            {
                ResourceName = ResourceName,
                BearerMethodsSupported = ["header"],
            };
            options.Events.OnResourceMetadataRequest = NameAuthorizationServer;
        });
        services.Configure<AccessTokenChallenge>(challenge => challenge.Scheme = McpAuthenticationDefaults.AuthenticationScheme);
    }

    private static Task NameAuthorizationServer(ResourceMetadataRequestContext context)
    {
        var request = context.HttpContext.Request;
        var origin = $"{request.Scheme}://{request.Host}";

        context.ResourceMetadata!.AuthorizationServers = [origin];

        return Task.CompletedTask;
    }

    private static void TrustHomeAssistantCoreForwarding(WebApplicationBuilder builder)
    {
        var configured = builder.Configuration[CoreProxySetting];
        var coreProxy = IPAddress.TryParse(configured, out var address) ? address : DefaultCoreProxy;

        builder.Services.Configure<ForwardedHeadersOptions>(forwarding =>
        {
            forwarding.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
            forwarding.KnownProxies.Add(coreProxy);
        });
    }
}
