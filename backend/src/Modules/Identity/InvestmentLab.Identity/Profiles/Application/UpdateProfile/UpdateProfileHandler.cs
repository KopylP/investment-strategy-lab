using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Profiles.Application.UpdateProfile;

public sealed record UpdateProfileCommand(string DisplayCurrency, string Locale, string TimeZone);

public sealed class UpdateProfileHandler(IUserProfileRepository profiles)
{
    public async Task<Result<UserProfileDto>> Handle(UserId userId, UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (await profiles.FindAsync(userId, cancellationToken) is not { } profile)
            return ProfileErrors.NotFound(userId);

        var currency = Currency.TryOf(command.DisplayCurrency);
        if (currency.IsFailure)
            return currency.Error;

        var updated = profile.Update(currency.Value, command.Locale, command.TimeZone);
        if (updated.IsFailure)
            return updated.Error;

        await profiles.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}
