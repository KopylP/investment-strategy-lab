using InvestmentLab.Identity.Profiles.Application.Shared;
using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Users.Domain;

namespace InvestmentLab.Identity.Profiles.Application.UpdateProfile;

public sealed record UpdateProfileCommand(string DisplayCurrency, string? Language, string TimeZone)
{
    public Result<Language?> ChosenLanguage()
    {
        if (string.IsNullOrWhiteSpace(Language))
            return Result<Language?>.Success(null);

        var language = SharedKernel.Language.TryOf(Language);

        return language.IsSuccess ? language.Value : language.Error;
    }
}

public sealed class UpdateProfileHandler(IUserProfileRepository profiles)
{
    public async Task<Result<UserProfileDto>> Handle(UserId userId, UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (await profiles.FindAsync(userId, cancellationToken) is not { } profile)
            return ProfileErrors.NotFound(userId);

        var currency = Currency.TryOf(command.DisplayCurrency);
        if (currency.IsFailure)
            return currency.Error;

        var language = command.ChosenLanguage();
        if (language.IsFailure)
            return language.Error;

        var updated = profile.Update(currency.Value, language.Value, command.TimeZone);
        if (updated.IsFailure)
            return updated.Error;

        await profiles.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}
