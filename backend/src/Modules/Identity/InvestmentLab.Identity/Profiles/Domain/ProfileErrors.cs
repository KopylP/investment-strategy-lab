namespace InvestmentLab.Identity.Profiles.Domain;

public static class ProfileErrors
{
    public static Error NotFound(UserId userId) => Error.NotFound("profile.not_found", $"User {userId} has no profile.");

    public static Error UnsupportedCurrency(Currency currency) =>
        Error.Validation("profile.currency_unsupported", $"Totals can be shown in EUR, USD or PLN, not {currency}.");

    public static Error UnknownTimeZone(string timeZone) =>
        Error.Validation("profile.time_zone_unknown", $"'{timeZone}' is not a known IANA time zone.");

    public static Error UnknownChartStyle(string chartStyle) =>
        Error.Validation("profile.chart_style_unknown", $"'{chartStyle}' is not a chart style; use Line or Candles.");
}
