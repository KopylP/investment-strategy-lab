namespace InvestmentLab.Identity.Profiles.Domain;

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

    public void Registered(string email, DateTimeOffset now) => Raise(new UserRegistered(Id, email, now));

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
            return !string.IsNullOrWhiteSpace(
                locale) && System.Globalization.CultureInfo.GetCultureInfo(locale,
                predefinedOnly: true).Name.Length > 0;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return false;
        }
    }
}
