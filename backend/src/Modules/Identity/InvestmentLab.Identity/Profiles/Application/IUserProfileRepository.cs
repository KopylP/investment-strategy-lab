using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Profiles.Application;

public interface IUserProfileRepository
{
    Task<UserProfile?> FindAsync(UserId userId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record UserProfileDto(string DisplayCurrency, string Locale, string TimeZone);

internal static class UserProfileMapping
{
    extension(UserProfile profile)
    {
        public UserProfileDto ToDto() => new(profile.DisplayCurrency.Code, profile.Locale, profile.TimeZone);
    }
}
