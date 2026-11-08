using System.Security.Claims;
using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.Identity.Users.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class AccessTokenEvents(UserManager<User> users, SignInManager<User> signIn, IOptions<AccessTokenChallenge> challenge) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var provider = context.Principal?.FindFirstValue(ExternalAccessTokenHandler.LoginProviderClaim);
        var externalUser = context.Principal?.FindFirstValue(ExternalAccessTokenHandler.ExternalUserClaim);
        if (provider is null || externalUser is null)
        {
            context.Fail("The access token names no external user.");
            return;
        }

        var user = await users.FindByLoginAsync(provider, externalUser);
        if (user is null)
        {
            context.Fail(new AccessTokenNotLinkedException());
            return;
        }

        context.Principal = await signIn.CreateUserPrincipalAsync(user);
    }

    public override async Task Challenge(JwtBearerChallengeContext context)
    {
        if (context.AuthenticateFailure is AccessTokenNotLinkedException)
        {
            context.HandleResponse();
            await RespondNotLinkedAsync(context.HttpContext);
        }
        else if (challenge.Value.Scheme is { } scheme)
        {
            context.HandleResponse();
            await context.HttpContext.ChallengeAsync(scheme, context.Properties);
        }
    }

    private static async Task RespondNotLinkedAsync(HttpContext http)
    {
        var error = AccessTokenErrors.NotLinked();
        var localized = MessageLocalizer.In(McpReply.Language).Render(error.AsMessage());

        var problem = TypedResults.Problem(
            detail: localized,
            statusCode: StatusCodes.Status403Forbidden,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });

        await problem.ExecuteAsync(http);
    }
}
