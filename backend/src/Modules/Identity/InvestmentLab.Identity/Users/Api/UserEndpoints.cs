using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Identity.Users.Application.GetCurrentUser;
using InvestmentLab.Identity.Users.Application.Login;
using InvestmentLab.Identity.Users.Application.Register;
using InvestmentLab.Identity.Users.Application.Shared;
using InvestmentLab.Identity.Users.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Identity.Users.Api;

internal static class UserEndpoints
{
    public static RouteGroupBuilder MapUsers(this RouteGroupBuilder group)
    {
        var users = group
            .MapGroup(string.Empty)
            .WithTags("Users");

        users
            .MapPost("/register", Register)
            .AllowAnonymous()
            .WithName("Register")
            .Produces<CurrentUserDto>();
        users
            .MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .Produces(StatusCodes.Status204NoContent);
        users
            .MapPost("/logout", Logout)
            .WithName("Logout");
        users
            .MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .Produces<CurrentUserDto>();

        return group;
    }

    private static async Task<IResult> Register(
        RegisterCommand body,
        RegisterHandler handler,
        CancellationToken cancellationToken)
    {
        var registered = await handler.Handle(body, cancellationToken);

        return registered.ToHttpResult();
    }

    private static async Task<IResult> Login(
        LoginCommand body,
        LoginHandler handler,
        CancellationToken cancellationToken)
    {
        var loggedIn = await handler.Handle(body, cancellationToken);

        return loggedIn.ToHttpResult();
    }

    private static async Task<NoContent> Logout(IUserAccounts accounts)
    {
        await accounts.SignOutAsync();

        return TypedResults.NoContent();
    }

    private static async Task<IResult> GetCurrentUser(
        HttpContext context,
        GetCurrentUserHandler handler,
        CancellationToken cancellationToken)
    {
        var viaHomeAssistant = IsSignedInThroughHomeAssistant(context);

        var current = await handler.Handle(viaHomeAssistant, cancellationToken);

        return current.ToHttpResult();
    }

    private static bool IsSignedInThroughHomeAssistant(HttpContext context) =>
        context.User.HasClaim(claim => claim.Type == HomeAssistantAuthenticationHandler.UserClaim);
}
