using InvestmentLab.Identity.Profiles.Application.Shared;
using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Profiles.Application.UpdateProfile;

public sealed record UpdateProfileCommand(string DisplayCurrency, string? Language, string TimeZone, string? ChartStyle = null)
{
    public Result<Language?> ChosenLanguage()
    {
        if (string.IsNullOrWhiteSpace(Language))
            return Result<Language?>.Success(null);

        var language = SharedKernel.Language.TryOf(Language);

        return language.IsSuccess ? language.Value : language.Error;
    }

    public Result<ChartStyle?> ChosenChartStyle()
    {
        if (string.IsNullOrWhiteSpace(ChartStyle))
            return Result<ChartStyle?>.Success(null);

        var parsed = Enum.TryParse<ChartStyle>(ChartStyle, ignoreCase: true, out var style) && Enum.IsDefined(style);

        return parsed ? style : ProfileErrors.UnknownChartStyle(ChartStyle);
    }
}

public sealed class UpdateProfileHandler(IUserProfileRepository profiles) : IHandler
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

        var chartStyle = command.ChosenChartStyle();
        if (chartStyle.IsFailure)
            return chartStyle.Error;

        var updated = profile.Update(currency.Value, language.Value, command.TimeZone, chartStyle.Value ?? profile.ChartStyle);
        if (updated.IsFailure)
            return updated.Error;

        await profiles.SaveChangesAsync(cancellationToken);

        return profile.ToDto();
    }
}
