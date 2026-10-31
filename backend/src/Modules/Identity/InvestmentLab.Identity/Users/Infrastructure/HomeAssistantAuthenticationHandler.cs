using System.Security.Claims;
using System.Text.Encodings.Web;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Shared.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class HomeAssistantAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    UserManager<User> users,
    SignInManager<User> signIn,
    UserStore store,
    IdentityDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "HomeAssistant";
    public const string LoginProvider = "HomeAssistant";

    public const string UserClaim = "investmentlab:home_assistant_user";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.HomeAssistantUserId() is not { } homeAssistantUser)
            return AuthenticateResult.NoResult();

        var user = await users.FindByLoginAsync(LoginProvider, homeAssistantUser) ?? await LinkSignedInUserAsync(homeAssistantUser);
        if (user is null)
            return AuthenticateResult.NoResult();

        var principal = await signIn.CreateUserPrincipalAsync(user);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(UserClaim, homeAssistantUser));

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private async Task<User?> LinkSignedInUserAsync(string homeAssistantUser)
    {
        var cookie = await Context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!cookie.Succeeded || await users.GetUserAsync(cookie.Principal) is not { } user)
            return null;

        var name = Request.Headers[HomeAssistantIngress.UserNameHeader].ToString();

        try
        {
            await store.AddLoginAsync(user, new UserLoginInfo(LoginProvider, homeAssistantUser, name), Context.RequestAborted);
            await db.SaveChangesAsync(Context.RequestAborted);
            Logger.LogInformation("Home Assistant user {HomeAssistantUser} ({Name}) now signs in as {UserId}", homeAssistantUser, name, user.Id);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }

        return await users.FindByLoginAsync(LoginProvider, homeAssistantUser);
    }
}
