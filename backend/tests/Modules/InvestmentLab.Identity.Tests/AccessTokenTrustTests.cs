using InvestmentLab.Identity.Users.Domain;

namespace InvestmentLab.Identity.Tests;

public sealed class AccessTokenTrustTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_token_is_trusted_without_asking_again_for_at_most_five_minutes()
    {
        var expiresInAnHour = Now.AddHours(1);

        AccessTokenTrust.For(expiresInAnHour, Now).ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void A_token_about_to_expire_is_trusted_only_until_it_expires()
    {
        var expiresInAMinute = Now.AddMinutes(1);

        AccessTokenTrust.For(expiresInAMinute, Now).ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void An_expired_token_is_never_remembered()
    {
        AccessTokenTrust.For(Now.AddSeconds(-1), Now).ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void A_token_without_a_known_expiry_is_never_remembered()
    {
        AccessTokenTrust.For(null, Now).ShouldBe(TimeSpan.Zero);
    }
}
