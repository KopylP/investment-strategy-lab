namespace InvestmentLab.Identity.Users.Domain;

public static class AccessTokenTrust
{
    public static readonly TimeSpan Longest = TimeSpan.FromMinutes(5);

    public static TimeSpan For(DateTimeOffset? tokenExpiry, DateTimeOffset now)
    {
        if (tokenExpiry is not { } expiry || expiry <= now)
            return TimeSpan.Zero;

        var left = expiry - now;

        return left < Longest ? left : Longest;
    }
}
