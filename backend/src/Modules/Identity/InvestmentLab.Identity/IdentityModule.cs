using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Contracts;
using InvestmentLab.Identity.Profiles.Api;
using InvestmentLab.Identity.Profiles.Application.GetProfile;
using InvestmentLab.Identity.Profiles.Application.UpdateProfile;
using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Profiles.Infrastructure;
using InvestmentLab.Identity.Shared.Infrastructure;
using InvestmentLab.Identity.Users.Api;
using InvestmentLab.Identity.Users.Application.GetCurrentUser;
using InvestmentLab.Identity.Users.Application.Integration;
using InvestmentLab.Identity.Users.Application.Login;
using InvestmentLab.Identity.Users.Application.Register;
using InvestmentLab.Identity.Users.Application.Shared;
using InvestmentLab.Identity.Users.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Identity;

public sealed class IdentityModule : IModule
{
    public const string Schema = IdentityDbContext.SchemaName;
    public const string CookieName = "investmentlab.auth";
    private const string AuthenticationScheme = "InvestmentLab";

    public string Name => "Identity";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<IdentityDbContext>(Schema);

        var services = builder.Services;

        services.AddAuthentication(AuthenticationScheme)
            .AddPolicyScheme(AuthenticationScheme, null, policy => policy.ForwardDefaultSelector = context =>
                context.HomeAssistantUserId() is null ? IdentityConstants.ApplicationScheme : HomeAssistantAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, HomeAssistantAuthenticationHandler>(HomeAssistantAuthenticationHandler.SchemeName, null)
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

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddDomainEventMapper<UserRegistered, UserRegisteredV1, UserRegisteredMapper>();
        services.AddScoped<IHomeAssistantUsers, HomeAssistantUsers>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<GetCurrentUserHandler>();

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateProfileHandler>();
    }

    private static Task Respond(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;

        return Task.CompletedTask;
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/identity")
            .MapUsers()
            .MapProfiles();
}
