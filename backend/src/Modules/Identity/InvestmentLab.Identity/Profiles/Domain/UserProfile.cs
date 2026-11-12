namespace InvestmentLab.Identity.Profiles.Domain;

public sealed class UserProfile : AggregateRoot<UserId>
{
    public static readonly Currency DefaultDisplayCurrency = Currency.EUR;
    public const string DefaultTimeZone = "Europe/Warsaw";

    private static readonly Currency[] DisplayCurrencies = [Currency.EUR, Currency.USD, Currency.PLN];

    public Currency DisplayCurrency { get; private set; }
    public Language? Language { get; private set; }
    public string TimeZone { get; private set; }
    public ChartStyle ChartStyle { get; private set; }

    private UserProfile(UserId id, Currency displayCurrency, Language? language, string timeZone, ChartStyle chartStyle) : base(id) =>
        (DisplayCurrency, Language, TimeZone, ChartStyle) = (displayCurrency, language, timeZone, chartStyle);

    public static UserProfile CreateDefault(UserId userId) => new(userId, DefaultDisplayCurrency, language: null, DefaultTimeZone, ChartStyle.Line);

    public void Registered(string email, DateTimeOffset now) => Raise(new UserRegistered(Id, email, now));

    public Result Update(Currency displayCurrency, Language? language, string timeZone, ChartStyle chartStyle)
    {
        if (!DisplayCurrencies.Contains(displayCurrency))
            return ProfileErrors.UnsupportedCurrency(displayCurrency);

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _))
            return ProfileErrors.UnknownTimeZone(timeZone);

        (DisplayCurrency, Language, TimeZone, ChartStyle) = (displayCurrency, language, timeZone, chartStyle);

        return Result.Success();
    }
}
