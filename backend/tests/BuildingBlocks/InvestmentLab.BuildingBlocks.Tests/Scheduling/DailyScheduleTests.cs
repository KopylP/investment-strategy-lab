using InvestmentLab.BuildingBlocks.Scheduling;

namespace InvestmentLab.BuildingBlocks.Tests.Scheduling;

public sealed class DailyScheduleTests
{
    private static readonly TimeZoneInfo Warsaw = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

    private static readonly DailySchedule NightlyOnWeekdays = new(new TimeOnly(22, 30), Warsaw, WeekdaysOnly: true);

    [Fact]
    public void Runs_later_the_same_day_when_the_time_has_not_come_yet()
    {
        var next = NightlyOnWeekdays.NextOccurrenceAfter(new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero));

        next.ShouldBe(new DateTimeOffset(2026, 7, 15, 20, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Runs_the_next_day_once_the_time_has_passed()
    {
        var next = NightlyOnWeekdays.NextOccurrenceAfter(new DateTimeOffset(2026, 7, 15, 21, 0, 0, TimeSpan.Zero));

        next.ShouldBe(new DateTimeOffset(2026, 7, 16, 20, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void An_occurrence_exactly_now_is_not_repeated()
    {
        var next = NightlyOnWeekdays.NextOccurrenceAfter(new DateTimeOffset(2026, 7, 15, 20, 30, 0, TimeSpan.Zero));

        next.ShouldBe(new DateTimeOffset(2026, 7, 16, 20, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Weekdays_only_skips_the_weekend()
    {
        var next = NightlyOnWeekdays.NextOccurrenceAfter(new DateTimeOffset(2026, 7, 17, 21, 0, 0, TimeSpan.Zero));

        next.ShouldBe(new DateTimeOffset(2026, 7, 20, 20, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Every_day_schedule_runs_on_the_weekend()
    {
        var daily = NightlyOnWeekdays with { WeekdaysOnly = false };

        var next = daily.NextOccurrenceAfter(new DateTimeOffset(2026, 7, 17, 21, 0, 0, TimeSpan.Zero));

        next.ShouldBe(new DateTimeOffset(2026, 7, 18, 20, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Local_time_follows_the_winter_offset()
    {
        var next = NightlyOnWeekdays.NextOccurrenceAfter(new DateTimeOffset(2026, 1, 14, 8, 0, 0, TimeSpan.Zero));

        next.ShouldBe(new DateTimeOffset(2026, 1, 14, 21, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_time_skipped_by_the_clock_change_runs_right_after_the_jump()
    {
        var earlyMorning = new DailySchedule(new TimeOnly(2, 30), Warsaw, WeekdaysOnly: false);

        var next = earlyMorning.NextOccurrenceAfter(new DateTimeOffset(2026, 3, 28, 23, 0, 0, TimeSpan.Zero));

        next.ShouldBe(new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_time_repeated_by_the_clock_change_runs_once_at_its_first_occurrence()
    {
        var earlyMorning = new DailySchedule(new TimeOnly(2, 30), Warsaw, WeekdaysOnly: false);

        var first = earlyMorning.NextOccurrenceAfter(new DateTimeOffset(2026, 10, 24, 23, 0, 0, TimeSpan.Zero));
        var afterFirst = earlyMorning.NextOccurrenceAfter(first);

        first.ShouldBe(new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero));
        afterFirst.ShouldBe(new DateTimeOffset(2026, 10, 26, 1, 30, 0, TimeSpan.Zero));
    }
}
