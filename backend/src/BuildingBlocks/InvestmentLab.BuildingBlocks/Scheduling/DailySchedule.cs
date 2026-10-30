namespace InvestmentLab.BuildingBlocks.Scheduling;

/// <summary>Once a day at a wall-clock time of a time zone, optionally only Monday to Friday.</summary>
public sealed record DailySchedule(TimeOnly At, TimeZoneInfo TimeZone, bool WeekdaysOnly) : IJobSchedule
{
    public static DailySchedule Weekdays(TimeOnly at, string timeZoneId) =>
        new(at, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId), WeekdaysOnly: true);

    /// <summary>The first occurrence strictly after <paramref name="instant"/>.</summary>
    public DateTimeOffset NextOccurrenceAfter(DateTimeOffset instant)
    {
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TimeZone).DateTime);

        while (true)
        {
            if (!(WeekdaysOnly && day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                && InstantOn(day) is var candidate && candidate > instant)
                return candidate;

            day = day.AddDays(1);
        }
    }

    private DateTimeOffset InstantOn(DateOnly day)
    {
        var local = day.ToDateTime(At, DateTimeKind.Unspecified);

        // Skipped by the spring clock change: keep the offset from before the jump (02:30 → 03:30).
        if (TimeZone.IsInvalidTime(local))
            return new DateTimeOffset(local, TimeZone.GetUtcOffset(local.AddHours(-3))).ToUniversalTime();

        // Repeated by the autumn clock change: run at the first occurrence only.
        if (TimeZone.IsAmbiguousTime(local))
            return new DateTimeOffset(local, TimeZone.GetAmbiguousTimeOffsets(local).Max()).ToUniversalTime();

        return new DateTimeOffset(local, TimeZone.GetUtcOffset(local)).ToUniversalTime();
    }
}
