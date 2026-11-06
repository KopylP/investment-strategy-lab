using System.Security.Claims;
using InvestmentLab.Identity.Users.Domain;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace InvestmentLab.IntegrationTests.Mcp;

internal sealed class FakeHomeAssistantTokens : IAccessTokenValidator
{
    public const string RejectedUser = "rejected";

    private const string LoginProvider = "HomeAssistant";
    private const string SubjectClaim = "sub";

    public static string TokenFor(string homeAssistantUser)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(SubjectClaim, homeAssistantUser)]),
            Expires = DateTime.UtcNow.AddMinutes(30),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public Task<ExternalSignIn?> ValidateAsync(string accessToken, CancellationToken cancellationToken)
    {
        var subject = new JsonWebToken(accessToken).Subject;
        var external = subject == RejectedUser ? null : new ExternalSignIn(LoginProvider, subject);

        return Task.FromResult(external);
    }
}
