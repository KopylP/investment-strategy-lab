using System.Globalization;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public enum ReportFrequency
{
    Off,
    Weekly,
    Monthly,
    Quarterly,
}

/// <summary>
/// When a strategy's report comes (its type declares it, its parameters may change how often): on Sunday evening, after
/// the markets closed on Friday, so the plan is carried out on Monday — every week, the first Sunday of the month or of the quarter.
/// </summary>
public sealed record ReportCadence(ReportFrequency Frequency, TimeOnly Time)
{
    public static ReportCadence Of(string frequency, string time) => new(
        Enum.TryParse<ReportFrequency>(frequency, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : ReportFrequency.Off,
        TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ? at : new TimeOnly(20, 0));

    public bool IsDue(DateOnly day) => day.DayOfWeek == DayOfWeek.Sunday && Frequency switch
    {
        ReportFrequency.Weekly => true,
        ReportFrequency.Monthly => day.Day <= 7,
        ReportFrequency.Quarterly => day.Day <= 7 && day.Month % 3 == 1,
        _ => false,
    };

    /// <summary>The first report day on or after <paramref name="from"/>; null when the report is off.</summary>
    public DateOnly? Next(DateOnly from)
    {
        if (Frequency == ReportFrequency.Off)
            return null;
        var day = from;
        while (!IsDue(day))
            day = day.AddDays(1);
        return day;
    }
}
