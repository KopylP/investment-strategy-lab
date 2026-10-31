using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Profiles.Application.Shared;

internal static class UserProfileMapping
{
    extension(UserProfile profile)
    {
        public UserProfileDto ToDto() => new(profile.DisplayCurrency.Code, profile.Locale, profile.TimeZone);
    }
}
