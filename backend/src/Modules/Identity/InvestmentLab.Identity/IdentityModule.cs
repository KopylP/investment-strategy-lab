using System.Reflection;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Contracts;
using InvestmentLab.Identity.Profiles.Api;
using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Profiles.Infrastructure;
using InvestmentLab.Identity.Shared.Infrastructure;
using InvestmentLab.Identity.Users.Api;
using InvestmentLab.Identity.Users.Application.Shared;
using InvestmentLab.Identity.Users.Domain;
using InvestmentLab.Identity.Users.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Identity;

public sealed class IdentityModule : IModule
{
    public const string Schema = IdentityDbContext.SchemaName;
    public const string CookieName = "investmentlab.auth";
    public const string AccessTokenScheme = "AccessToken";
    public const string AccessTokenIssuerSetting = "Identity:AccessTokens:Issuer";
    private const string AuthenticationScheme = "InvestmentLab";
    private const string HomeAssistantIssuer = "HomeAssistant";

    public string Name => "Identity";

    public Assembly Assembly => typeof(IdentityAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<IdentityDbContext>(Schema);

        var services = builder.Services;
        AddAuthentication(services);
        AddAccessTokens(builder);
        AddUsers(services);
        AddAuthorization(services);
        AddHandlers(services);
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(AuthenticationScheme)
                    .AddPolicyScheme(AuthenticationScheme, null, policy => policy.ForwardDefaultSelector = SchemeOf)
                    .AddScheme<AuthenticationSchemeOptions, HomeAssistantAuthenticationHandler>(HomeAssistantAuthenticationHandler.SchemeName, null)
                    .AddJwtBearer(AccessTokenScheme, bearer => bearer.EventsType = typeof(AccessTokenEvents))
                    .AddIdentityCookies();
        services.ConfigureApplicationCookie(cookie =>
        {
            cookie.Cookie.Name = CookieName;
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SameSite = SameSiteMode.Strict;
            cookie.ExpireTimeSpan = TimeSpan.FromDays(14);
            cookie.SlidingExpiration = true;

            cookie.Events.OnRedirectToLogin = context => Respond(context.Response, StatusCodes.Status401Unauthorized);
            cookie.Events.OnRedirectToAccessDenied = context => Respond(context.Response, StatusCodes.Status403Forbidden);
        });
    }

    private static string SchemeOf(HttpContext context)
    {
        if (CarriesBearerToken(context))
            return AccessTokenScheme;

        return context.HomeAssistantUserId() is null ? IdentityConstants.ApplicationScheme : HomeAssistantAuthenticationHandler.SchemeName;
    }

    private static bool CarriesBearerToken(HttpContext context) =>
        context.Request.Headers.Authorization.ToString().StartsWith(JwtBearerDefaults.AuthenticationScheme + " ", StringComparison.OrdinalIgnoreCase);

    private static void AddAccessTokens(IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<AccessTokenEvents>();
        builder.Services.AddOptions<AccessTokenChallenge>();

        var issuer = builder.Configuration[AccessTokenIssuerSetting];
        if (string.IsNullOrEmpty(issuer))
            return;
        if (issuer != HomeAssistantIssuer)
            throw new InvalidOperationException($"Unknown access token issuer '{issuer}' in {AccessTokenIssuerSetting}.");

        AddHomeAssistantAccessTokens(builder);
    }

    private static void AddHomeAssistantAccessTokens(IHostApplicationBuilder builder)
    {
        var address = builder.Configuration.GetSection(HomeAssistantCoreOptions.Section)["Address"] ?? HomeAssistantCoreOptions.DefaultAddress;

        builder.Services.AddHybridCache();
        builder.Services.AddSingleton(new HomeAssistantCoreOptions(new Uri(address)));
        builder.Services.AddSingleton<HomeAssistantAccessTokenValidator>();
        builder.Services.AddSingleton<IAccessTokenValidator>(provider => new CachedAccessTokenValidator(
            issuer: provider.GetRequiredService<HomeAssistantAccessTokenValidator>(),
            cache: provider.GetRequiredService<HybridCache>(),
            clock: provider.GetRequiredService<TimeProvider>()));
        builder.Services.AddOptions<JwtBearerOptions>(AccessTokenScheme)
            .Configure<IAccessTokenValidator>(ValidateThrough);
    }

    private static void ValidateThrough(JwtBearerOptions bearer, IAccessTokenValidator validator)
    {
        bearer.TokenHandlers.Clear();
        bearer.TokenHandlers.Add(new ExternalAccessTokenHandler(validator));
    }

    private static void AddUsers(IServiceCollection services)
    {
        services.AddIdentityCore<User>(identity =>
                    {
                        identity.User.RequireUniqueEmail = true;
                        identity.Password.RequiredLength = 10;
                        identity.Password.RequireNonAlphanumeric = false;
                        identity.Password.RequireUppercase = false;
                        identity.Password.RequireDigit = false;
                        identity.Lockout.MaxFailedAccessAttempts = 5;
                    })
                    .AddSignInManager();
        services.AddScoped<UserStore>();
        services.AddScoped<IUserStore<User>>(provider => provider.GetRequiredService<UserStore>());
    }

    private static void AddAuthorization(IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
                    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddScoped<IHomeAssistantUsers, HomeAssistantUsers>();

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserLanguages, UserLanguages>();
    }

    private static Task Respond(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;

        return Task.CompletedTask;
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints
            .MapGroup("/api/identity")
            .MapUsers()
            .MapProfiles();
}
