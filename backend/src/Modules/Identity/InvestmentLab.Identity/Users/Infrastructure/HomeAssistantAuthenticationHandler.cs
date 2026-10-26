using System.Security.Claims;
using System.Text.Encodings.Web;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Shared.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestmentLab.Identity.Users.Infrastructure;

/// <summary>
/// Signs in the Home Assistant user behind the add-on's sidebar panel, without a password. The first time a Home
/// Assistant user comes through, whoever is signed in there with the cookie (an earlier session, or the login form once)
/// becomes their account: the link is a <c>HomeAssistant</c> external login, so later visits need no form at all.
/// </summary>
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

    /// <summary>On a principal signed in through the panel: the Home Assistant user id.</summary>
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

        // Through the store, not UserManager.AddLoginAsync: that renews the security stamp and so signs the user out
        // of their other browsers.
        var name = Request.Headers[HomeAssistantIngress.UserNameHeader].ToString();
        try
        {
            await store.AddLoginAsync(user, new UserLoginInfo(LoginProvider, homeAssistantUser, name), Context.RequestAborted);
            await db.SaveChangesAsync(Context.RequestAborted);
            Logger.LogInformation("Home Assistant user {HomeAssistantUser} ({Name}) now signs in as {UserId}", homeAssistantUser, name, user.Id);
        }
        catch (DbUpdateException)
        {
            // Two first requests at once: the other one linked them.
            db.ChangeTracker.Clear();
        }
        return await users.FindByLoginAsync(LoginProvider, homeAssistantUser);
    }
}
