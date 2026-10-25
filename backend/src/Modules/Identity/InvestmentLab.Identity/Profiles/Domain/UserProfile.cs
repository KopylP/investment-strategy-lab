namespace InvestmentLab.Identity.Profiles.Domain;

/// <summary>How a user wants to see their data: currency of totals, language and time zone of dates.</summary>
public sealed class UserProfile : AggregateRoot<UserId>
{
    public static readonly Currency DefaultDisplayCurrency = Currency.EUR;
    public const string DefaultLocale = "uk-UA";
    public const string DefaultTimeZone = "Europe/Warsaw";

    private static readonly Currency[] DisplayCurrencies = [Currency.EUR, Currency.USD, Currency.PLN];

    public Currency DisplayCurrency { get; private set; }
    public string Locale { get; private set; }
    public string TimeZone { get; private set; }

    private UserProfile(UserId id, Currency displayCurrency, string locale, string timeZone) : base(id) =>
        (DisplayCurrency, Locale, TimeZone) = (displayCurrency, locale, timeZone);

    public static UserProfile CreateDefault(UserId userId) => new(userId, DefaultDisplayCurrency, DefaultLocale, DefaultTimeZone);

    public Result Update(Currency displayCurrency, string locale, string timeZone)
    {
        if (!DisplayCurrencies.Contains(displayCurrency))
            return ProfileErrors.UnsupportedCurrency(displayCurrency);
        if (!IsKnownLocale(locale))
            return ProfileErrors.UnknownLocale(locale);
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            return ProfileErrors.UnknownTimeZone(timeZone);

        (DisplayCurrency, Locale, TimeZone) = (displayCurrency, locale, timeZone);
        return Result.Success();
    }

    private static bool IsKnownLocale(string locale)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(locale) && System.Globalization.CultureInfo.GetCultureInfo(locale, predefinedOnly: true).Name.Length > 0;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return false;
        }
    }
}

public static class ProfileErrors
{
    public static Error NotFound(UserId userId) => Error.NotFound("profile.not_found", $"User {userId} has no profile.");

    public static Error UnsupportedCurrency(Currency currency) =>
        Error.Validation("profile.currency_unsupported", $"Totals can be shown in EUR, USD or PLN, not {currency}.");

    public static Error UnknownLocale(string locale) => Error.Validation("profile.locale_unknown", $"'{locale}' is not a known locale.");

    public static Error UnknownTimeZone(string timeZone) => Error.Validation("profile.time_zone_unknown", $"'{timeZone}' is not a known IANA time zone.");
}
