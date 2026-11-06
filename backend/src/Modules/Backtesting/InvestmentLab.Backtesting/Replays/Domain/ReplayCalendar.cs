namespace InvestmentLab.Backtesting.Replays.Domain;

public static class ReplayCalendar
{
    public static bool IsDue(ReplaySchedule schedule, DateOnly? previous, DateOnly day)
    {
        if (previous is null) return true; // Seed the portfolio when its warmup is complete.
        if (schedule.Cadence == ReplayCadence.Daily) return day > previous;
        DateOnly due;
        if (schedule.Cadence == ReplayCadence.Weekly)
        {
            var daysSince = ((int)day.DayOfWeek - (int)schedule.Weekday + 7) % 7;
            due = day.AddDays(-daysSince);
        }
        else
        {
            var firstMonth = schedule.Cadence == ReplayCadence.Quarterly ? (day.Month - 1) / 3 * 3 + 1 : day.Month;
            var month = new DateOnly(day.Year, firstMonth, 1);
            due = month.AddDays(Math.Clamp(schedule.Day, 1, DateTime.DaysInMonth(month.Year, month.Month)) - 1);
            if (due > day)
            {
                month = month.AddMonths(schedule.Cadence == ReplayCadence.Quarterly ? -3 : -1);
                due = month.AddDays(Math.Clamp(schedule.Day, 1, DateTime.DaysInMonth(month.Year, month.Month)) - 1);
            }
        }
        // A holiday moves the action to the first available session after its due date.
        return due > previous && due <= day;
    }

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
