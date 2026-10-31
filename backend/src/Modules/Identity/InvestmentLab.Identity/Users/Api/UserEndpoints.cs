using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Users.Application;
using InvestmentLab.Identity.Users.Application.Login;
using InvestmentLab.Identity.Users.Application.Register;
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
        var users = group.MapGroup("").WithTags("Users");

        users.MapPost("/register", Register).AllowAnonymous().WithName("Register");
        users.MapPost("/login", Login).AllowAnonymous().WithName("Login");
        users.MapPost("/logout", Logout).WithName("Logout");
        users.MapGet("/me", GetCurrentUser).WithName("GetCurrentUser");

        return group;
    }

    private static async Task<IResult> Register(RegisterCommand body, RegisterHandler handler, CancellationToken cancellationToken)
    {
        var registered = await handler.Handle(body, cancellationToken);

        return registered.ToHttpResult();
    }

    private static async Task<IResult> Login(LoginCommand body, LoginHandler handler, CancellationToken cancellationToken)
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
        HttpContext context, ICurrentUser user, IUserAccounts accounts, CancellationToken cancellationToken)
    {
        if (await accounts.FindAsync(user.RequiredId, cancellationToken) is not { } me)
            return UserErrors.NotFound(user.RequiredId).ToProblem();

        var viaHomeAssistant = context.User.HasClaim(claim => claim.Type == HomeAssistantAuthenticationHandler.UserClaim);
        var current = me with { ViaHomeAssistant = viaHomeAssistant };

        return Result<CurrentUserDto>.Success(current).ToHttpResult();
    }
}
