namespace InvestmentLab.BuildingBlocks.Scheduling;

public sealed record DailySchedule(TimeOnly At, TimeZoneInfo TimeZone, bool WeekdaysOnly) : IJobSchedule
{
    public static DailySchedule Weekdays(TimeOnly at, string timeZoneId) =>
        new(at, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId), WeekdaysOnly: true);

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
        if (TimeZone.IsInvalidTime(local))
            return new DateTimeOffset(local, TimeZone.GetUtcOffset(local.AddHours(-3))).ToUniversalTime();

        if (TimeZone.IsAmbiguousTime(local))
            return new DateTimeOffset(local, TimeZone.GetAmbiguousTimeOffsets(local).Max()).ToUniversalTime();

        return new DateTimeOffset(local, TimeZone.GetUtcOffset(local)).ToUniversalTime();
    }
}
