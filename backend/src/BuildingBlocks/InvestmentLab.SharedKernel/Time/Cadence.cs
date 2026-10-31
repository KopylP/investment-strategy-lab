using System.Globalization;

namespace InvestmentLab.SharedKernel;

public sealed record Cadence(CadenceFrequency Frequency, int Day, DayOfWeek Weekday, TimeOnly Time, int Minute)
{
    public static readonly Cadence Off = new(CadenceFrequency.Off, 1, DayOfWeek.Sunday, new TimeOnly(20, 0), 0);

    public static Cadence Of(string frequency, int day, string weekday, string time, int minute)
    {
        var parsed = Enum.TryParse<CadenceFrequency>(frequency, ignoreCase: true, out var known) && Enum.IsDefined(known)
            ? known
            : CadenceFrequency.Off;

        return new Cadence(
            parsed,
            parsed is CadenceFrequency.Monthly or CadenceFrequency.Quarterly && day is >= 1 and <= 31 ? day : Off.Day,
            parsed == CadenceFrequency.Weekly && Enum.TryParse<DayOfWeek>(weekday, ignoreCase: true, out var on) && Enum.IsDefined(on)
                ? on
                : Off.Weekday,
            parsed is not (CadenceFrequency.Off or CadenceFrequency.Hourly)
            && TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
                ? at
                : Off.Time,
            parsed == CadenceFrequency.Hourly && minute is >= 0 and <= 59 ? minute : Off.Minute);
    }

    public static Result<Cadence> TryOf(string frequency, int? day, string? weekday, string? time, int? minute)
    {
        if (!Enum.TryParse<CadenceFrequency>(frequency, ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed)
            || int.TryParse(frequency, out _))
            return Error.Validation("cadence.frequency", $"Unknown frequency '{frequency}': off, hourly, daily, weekly, monthly or quarterly.");

        if (parsed is CadenceFrequency.Monthly or CadenceFrequency.Quarterly && day is not (>= 1 and <= 31))
            return Error.Validation("cadence.day", "The day of the month must be 1–31.");

        if (parsed == CadenceFrequency.Weekly
            && (weekday is null || int.TryParse(weekday, out _) || !Enum.TryParse<DayOfWeek>(weekday, ignoreCase: true, out _)))
            return Error.Validation("cadence.weekday", "The weekday must be monday … sunday.");

        if (parsed is not (CadenceFrequency.Off or CadenceFrequency.Hourly)
            && !TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return Error.Validation("cadence.time", "The time must be HH:mm.");
        if (parsed == CadenceFrequency.Hourly && minute is not (>= 0 and <= 59))
            return Error.Validation("cadence.minute", "The minute must be 0–59.");

        return Of(frequency, day ?? Off.Day, weekday ?? "", time ?? "", minute ?? Off.Minute);
    }

    public DateTime? Last(DateTime now)
    {
        if (Frequency == CadenceFrequency.Off)
            return null;

        var period = PeriodOf(now);
        var at = At(period);

        return at <= now ? at : At(Step(period, -1));
    }

    public DateTime? Next(DateTime now)
    {
        if (Frequency == CadenceFrequency.Off)
            return null;

        var period = PeriodOf(now);
        var at = At(period);

        return at > now ? at : At(Step(period, 1));
    }

    private DateTime PeriodOf(DateTime instant) => Frequency switch
    {
        CadenceFrequency.Hourly => instant.Date.AddHours(instant.Hour),
        CadenceFrequency.Daily => instant.Date,
        CadenceFrequency.Weekly => instant.Date.AddDays(-(((int)instant.DayOfWeek + 6) % 7)),
        CadenceFrequency.Monthly => new DateTime(instant.Year, instant.Month, 1),
        _ => new DateTime(instant.Year, (instant.Month - 1) / 3 * 3 + 1, 1),
    };

    private DateTime Step(DateTime period, int count) => Frequency switch
    {
        CadenceFrequency.Hourly => period.AddHours(count),
        CadenceFrequency.Daily => period.AddDays(count),
        CadenceFrequency.Weekly => period.AddDays(7 * count),
        CadenceFrequency.Monthly => period.AddMonths(count),
        _ => period.AddMonths(3 * count),
    };

    private DateTime At(DateTime period) => Frequency switch
    {
        CadenceFrequency.Hourly => period.AddMinutes(Minute),
        CadenceFrequency.Daily => period + Time.ToTimeSpan(),
        CadenceFrequency.Weekly => period.AddDays(((int)Weekday + 6) % 7) + Time.ToTimeSpan(),
        _ => period.AddDays(Math.Min(Day, DateTime.DaysInMonth(period.Year, period.Month)) - 1) + Time.ToTimeSpan(),
    };
}
