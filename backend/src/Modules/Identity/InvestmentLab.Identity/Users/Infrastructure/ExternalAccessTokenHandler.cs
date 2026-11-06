using System.Security.Claims;
using InvestmentLab.Identity.Users.Domain;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class ExternalAccessTokenHandler(IAccessTokenValidator validator) : TokenHandler
{
    public const string LoginProviderClaim = "investmentlab:login_provider";
    public const string ExternalUserClaim = "investmentlab:external_user";

    private const string AuthenticationType = "AccessToken";

    public override async Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
    {
        var external = await validator.ValidateAsync(token, CancellationToken.None);
        if (external is null)
            return Rejected();

        return new TokenValidationResult
        {
            IsValid = true,
            ClaimsIdentity = IdentityOf(external),
            SecurityToken = new JsonWebToken(token),
        };
    }

    private static ClaimsIdentity IdentityOf(ExternalSignIn external)
    {
        Claim[] claims =
        [
            new(LoginProviderClaim, external.LoginProvider),
            new(ExternalUserClaim, external.ExternalUserId),
        ];

        return new ClaimsIdentity(claims, AuthenticationType);
    }

    private static TokenValidationResult Rejected() =>
        new()
        {
            IsValid = false,
            Exception = new SecurityTokenValidationException("The access token was not accepted by its issuer."),
        };
}
