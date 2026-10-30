using System.Globalization;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public enum ReportFrequency
{
    Off,
    Hourly,
    Daily,
    Weekly,
    Monthly,
    Quarterly,
}

/// <summary>
/// When a report comes, in the service's local time (its strategy's type declares a default, the user may set their own
/// for the account):
/// every hour at <see cref="Minute"/>; every day, on <see cref="Weekday"/> each week, or on <see cref="Day"/> of each
/// month or of the quarter's first month (a shorter month: its last day) at <see cref="Time"/>.
/// </summary>
public sealed record ReportCadence(ReportFrequency Frequency, int Day, DayOfWeek Weekday, TimeOnly Time, int Minute)
{
    public static readonly ReportCadence Off = new(ReportFrequency.Off, 1, DayOfWeek.Sunday, new TimeOnly(20, 0), 0);

    /// <summary>
    /// An unknown frequency is off; a field out of range, or one the frequency does not read, keeps its default (so two
    /// strategies with the same schedule have equal cadences).
    /// </summary>
    public static ReportCadence Of(string frequency, int day, string weekday, string time, int minute)
    {
        var parsed = Enum.TryParse<ReportFrequency>(frequency, ignoreCase: true, out var known) && Enum.IsDefined(known) ? known : ReportFrequency.Off;
        return new ReportCadence(
            parsed,
            parsed is ReportFrequency.Monthly or ReportFrequency.Quarterly && day is >= 1 and <= 31 ? day : Off.Day,
            parsed == ReportFrequency.Weekly && Enum.TryParse<DayOfWeek>(weekday, ignoreCase: true, out var on) && Enum.IsDefined(on) ? on : Off.Weekday,
            parsed is not (ReportFrequency.Off or ReportFrequency.Hourly) && TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
                ? at
                : Off.Time,
            parsed == ReportFrequency.Hourly && minute is >= 0 and <= 59 ? minute : Off.Minute);
    }

    /// <summary>
    /// The user's own schedule, strictly: the frequency and the fields it reads must be valid (a monthly one needs a day
    /// and a time, an hourly one a minute); fields it does not read are ignored.
    /// </summary>
    public static Result<ReportCadence> TryOf(string frequency, int? day, string? weekday, string? time, int? minute)
    {
        if (!Enum.TryParse<ReportFrequency>(frequency, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed) || int.TryParse(frequency, out _))
            return Error.Validation("report_schedule.frequency", $"Unknown frequency '{frequency}': off, hourly, daily, weekly, monthly or quarterly.");
        if (parsed is ReportFrequency.Monthly or ReportFrequency.Quarterly && day is not (>= 1 and <= 31))
            return Error.Validation("report_schedule.day", "The day of the month must be 1–31.");
        if (parsed == ReportFrequency.Weekly && (weekday is null || int.TryParse(weekday, out _) || !Enum.TryParse<DayOfWeek>(weekday, ignoreCase: true, out _)))
            return Error.Validation("report_schedule.weekday", "The weekday must be monday … sunday.");
        if (parsed is not (ReportFrequency.Off or ReportFrequency.Hourly)
            && !TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return Error.Validation("report_schedule.time", "The time must be HH:mm.");
        if (parsed == ReportFrequency.Hourly && minute is not (>= 0 and <= 59))
            return Error.Validation("report_schedule.minute", "The minute must be 0–59.");
        return Of(frequency, day ?? Off.Day, weekday ?? "", time ?? "", minute ?? Off.Minute);
    }

    /// <summary>The latest report time at or before <paramref name="now"/> (local); null when the report is off.</summary>
    public DateTime? Last(DateTime now)
    {
        if (Frequency == ReportFrequency.Off)
            return null;
        var period = PeriodOf(now);
        var at = At(period);
        return at <= now ? at : At(Step(period, -1));
    }

    /// <summary>The first report time after <paramref name="now"/> (local); null when the report is off.</summary>
    public DateTime? Next(DateTime now)
    {
        if (Frequency == ReportFrequency.Off)
            return null;
        var period = PeriodOf(now);
        var at = At(period);
        return at > now ? at : At(Step(period, 1));
    }

    /// <summary>The start of the hour, day, week (Monday), month or quarter holding <paramref name="instant"/>.</summary>
    private DateTime PeriodOf(DateTime instant) => Frequency switch
    {
        ReportFrequency.Hourly => instant.Date.AddHours(instant.Hour),
        ReportFrequency.Daily => instant.Date,
        ReportFrequency.Weekly => instant.Date.AddDays(-(((int)instant.DayOfWeek + 6) % 7)),
        ReportFrequency.Monthly => new DateTime(instant.Year, instant.Month, 1),
        _ => new DateTime(instant.Year, (instant.Month - 1) / 3 * 3 + 1, 1),
    };

    private DateTime Step(DateTime period, int count) => Frequency switch
    {
        ReportFrequency.Hourly => period.AddHours(count),
        ReportFrequency.Daily => period.AddDays(count),
        ReportFrequency.Weekly => period.AddDays(7 * count),
        ReportFrequency.Monthly => period.AddMonths(count),
        _ => period.AddMonths(3 * count),
    };

    private DateTime At(DateTime period) => Frequency switch
    {
        ReportFrequency.Hourly => period.AddMinutes(Minute),
        ReportFrequency.Daily => period + Time.ToTimeSpan(),
        ReportFrequency.Weekly => period.AddDays(((int)Weekday + 6) % 7) + Time.ToTimeSpan(),
        _ => period.AddDays(Math.Min(Day, DateTime.DaysInMonth(period.Year, period.Month)) - 1) + Time.ToTimeSpan(),
    };
}
