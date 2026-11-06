using System.Security.Cryptography;
using System.Text;
using InvestmentLab.Identity.Users.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.JsonWebTokens;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class CachedAccessTokenValidator(IAccessTokenValidator issuer, HybridCache cache, TimeProvider clock) : IAccessTokenValidator
{
    private const string KeyPrefix = "identity:access-token:";

    public async Task<ExternalSignIn?> ValidateAsync(string accessToken, CancellationToken cancellationToken)
    {
        var trust = AccessTokenTrust.For(ExpiryOf(accessToken), clock.GetUtcNow());
        if (trust == TimeSpan.Zero)
            return await issuer.ValidateAsync(accessToken, cancellationToken);

        var entry = new HybridCacheEntryOptions { Expiration = trust, LocalCacheExpiration = trust };

        return await cache.GetOrCreateAsync(
            KeyOf(accessToken),
            accessToken,
            (token, cancel) => new ValueTask<ExternalSignIn?>(issuer.ValidateAsync(token, cancel)),
            entry,
            cancellationToken: cancellationToken);
    }

    private static DateTimeOffset? ExpiryOf(string accessToken)
    {
        var tokens = new JsonWebTokenHandler();
        if (!tokens.CanReadToken(accessToken))
            return null;

        var validTo = tokens.ReadJsonWebToken(accessToken).ValidTo;

        return new DateTimeOffset(validTo, TimeSpan.Zero);
    }

    private static string KeyOf(string accessToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken));

        return KeyPrefix + Convert.ToHexString(hash);
    }
}
