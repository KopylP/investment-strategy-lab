using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class ReplayCalendarTests
{
    [Fact]
    public void Monthly_day_is_preserved_and_short_months_use_the_last_day()
    {
        var fifteenth = new ReplaySchedule(ReplayCadence.Monthly, Day: 15);
        ReplayCalendar.IsDue(fifteenth, new(2026, 10, 1), new(2026, 10, 2)).ShouldBeFalse();
        ReplayCalendar.IsDue(fifteenth, new(2026, 10, 14), new(2026, 10, 15)).ShouldBeTrue();
        ReplayCalendar.IsDue(new(ReplayCadence.Monthly, Day: 31), new(2024, 2, 28), new(2024, 2, 29)).ShouldBeTrue();
    }

    [Fact]
    public void Weekly_day_moves_to_the_next_available_session_after_a_holiday()
    {
        var friday = new ReplaySchedule(ReplayCadence.Weekly, Weekday: DayOfWeek.Friday);
        ReplayCalendar.IsDue(friday, new(2024, 3, 27), new(2024, 3, 28)).ShouldBeFalse();
        ReplayCalendar.IsDue(friday, new(2024, 3, 28), new(2024, 4, 1)).ShouldBeTrue();
        ReplayCalendar.IsDue(friday, new(2024, 4, 1), new(2024, 4, 2)).ShouldBeFalse();
    }

    [Fact]
    public void Quarterly_schedule_uses_the_day_of_the_first_month()
    {
        var schedule = new ReplaySchedule(ReplayCadence.Quarterly, Day: 15);
        ReplayCalendar.IsDue(schedule, new(2026, 4, 1), new(2026, 4, 2)).ShouldBeFalse();
        ReplayCalendar.IsDue(schedule, new(2026, 4, 14), new(2026, 4, 15)).ShouldBeTrue();
        ReplayCalendar.IsDue(schedule, new(2026, 5, 14), new(2026, 5, 15)).ShouldBeFalse();
    }
}
