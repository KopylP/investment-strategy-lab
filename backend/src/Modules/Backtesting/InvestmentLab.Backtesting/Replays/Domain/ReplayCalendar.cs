namespace InvestmentLab.Backtesting.Replays.Domain;

public static class ReplayCalendar
{
    private const int MonthsInQuarter = 3;

    public static bool OpensPeriod(ReplayCadence cadence, DateOnly? previous, DateOnly day)
    {
        if (previous is not { } before)
            return true;

        return cadence switch
        {
            ReplayCadence.Daily => true,
            ReplayCadence.Weekly => WeekOf(before) != WeekOf(day),
            ReplayCadence.Monthly => (before.Year, before.Month) != (day.Year, day.Month),
            ReplayCadence.Quarterly => QuarterOf(before) != QuarterOf(day),
            _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "Unknown replay cadence."),
        };
    }

    private static (int Year, int Week) WeekOf(DateOnly day)
    {
        var moment = day.ToDateTime(TimeOnly.MinValue);

        return (System.Globalization.ISOWeek.GetYear(moment), System.Globalization.ISOWeek.GetWeekOfYear(moment));
    }

    private static (int Year, int Quarter) QuarterOf(DateOnly day) => (day.Year, (day.Month - 1) / MonthsInQuarter);
}
