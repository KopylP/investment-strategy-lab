namespace InvestmentLab.Identity.Profiles.Domain;

public sealed class UserProfile : AggregateRoot<UserId>
{
    public static readonly Currency DefaultDisplayCurrency = Currency.EUR;
    public const string DefaultTimeZone = "Europe/Warsaw";

    private static readonly Currency[] DisplayCurrencies = [Currency.EUR, Currency.USD, Currency.PLN];

    public Currency DisplayCurrency { get; private set; }
    public Language? Language { get; private set; }
    public string TimeZone { get; private set; }

    private UserProfile(UserId id, Currency displayCurrency, Language? language, string timeZone) : base(id) =>
        (DisplayCurrency, Language, TimeZone) = (displayCurrency, language, timeZone);

    public static UserProfile CreateDefault(UserId userId) => new(userId, DefaultDisplayCurrency, language: null, DefaultTimeZone);

    public void Registered(string email, DateTimeOffset now) => Raise(new UserRegistered(Id, email, now));

    public Result Update(Currency displayCurrency, Language? language, string timeZone)
    {
        if (!DisplayCurrencies.Contains(displayCurrency))
            return ProfileErrors.UnsupportedCurrency(displayCurrency);

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            return ProfileErrors.UnknownTimeZone(timeZone);

        (DisplayCurrency, Language, TimeZone) = (displayCurrency, language, timeZone);

        return Result.Success();
    }
}
