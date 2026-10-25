using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Users.Application;
using InvestmentLab.Identity.Users.Application.Login;
using InvestmentLab.Identity.Users.Application.Register;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Identity.Users.Api;

internal static class UserEndpoints
{
    public static RouteGroupBuilder MapUsers(this RouteGroupBuilder group)
    {
        var users = group.MapGroup("").WithTags("Users");

        users.MapPost("/register", async (RegisterCommand body, RegisterHandler handler, CancellationToken ct) =>
                (await handler.Handle(body, ct)).ToHttpResult())
            .AllowAnonymous()
            .WithName("Register");

        users.MapPost("/login", async (LoginCommand body, LoginHandler handler, CancellationToken ct) =>
                (await handler.Handle(body, ct)).ToHttpResult())
            .AllowAnonymous()
            .WithName("Login");

        users.MapPost("/logout", async (IUserAccounts accounts) =>
            {
                await accounts.SignOutAsync();
                return TypedResults.NoContent();
            })
            .WithName("Logout");

        users.MapGet("/me", async (ICurrentUser user, IUserAccounts accounts, CancellationToken ct) =>
                await accounts.FindAsync(user.RequiredId, ct) is { } me
                    ? Result<CurrentUserDto>.Success(me).ToHttpResult()
                    : UserErrors.NotFound(user.RequiredId).ToProblem())
            .WithName("GetCurrentUser");

        return group;
    }
}
