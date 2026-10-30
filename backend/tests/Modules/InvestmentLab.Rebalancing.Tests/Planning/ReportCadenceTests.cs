using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class ReportCadenceTests
{
    private static ReportCadence Cadence(string frequency, int day = 1, string weekday = "sunday", string time = "20:00", int minute = 0) =>
        ReportCadence.Of(frequency, day, weekday, time, minute);

    private static DateTime At(string local) => DateTime.Parse(local, System.Globalization.CultureInfo.InvariantCulture);

    public static TheoryData<ReportCadence, string, string, string> Times => new()
    {
        // cadence, now, last at or before now, next after now
        { Cadence("hourly", minute: 15), "2026-11-04T10:20", "2026-11-04T10:15", "2026-11-04T11:15" },
        { Cadence("hourly", minute: 15), "2026-11-04T10:15", "2026-11-04T10:15", "2026-11-04T11:15" },
        { Cadence("hourly", minute: 45), "2026-11-04T00:10", "2026-11-03T23:45", "2026-11-04T00:45" },
        { Cadence("daily", time: "08:30"), "2026-11-04T08:00", "2026-11-03T08:30", "2026-11-04T08:30" },
        { Cadence("weekly", weekday: "friday", time: "18:00"), "2026-11-04T12:00", "2026-10-30T18:00", "2026-11-06T18:00" },  // a Wednesday
        { Cadence("weekly", weekday: "sunday", time: "19:00"), "2026-11-08T19:30", "2026-11-08T19:00", "2026-11-15T19:00" },
        { Cadence("monthly", day: 15, time: "09:00"), "2026-11-04T12:00", "2026-10-15T09:00", "2026-11-15T09:00" },
        { Cadence("monthly", day: 31), "2026-11-30T21:00", "2026-11-30T20:00", "2026-12-31T20:00" },  // November has 30 days
        { Cadence("monthly", day: 31), "2027-03-01T10:00", "2027-02-28T20:00", "2027-03-31T20:00" },
        { Cadence("quarterly", day: 5), "2026-11-04T12:00", "2026-10-05T20:00", "2027-01-05T20:00" },
    };

    [Theory]
    [MemberData(nameof(Times))]
    public void Report_times_follow_the_schedule_the_strategy_sets(ReportCadence cadence, string now, string last, string next)
    {
        cadence.Last(At(now)).ShouldBe(At(last));
        cadence.Next(At(now)).ShouldBe(At(next));
    }

    [Fact]
    public void A_report_switched_off_has_no_times() =>
        Cadence("off").Next(At("2026-11-04T12:00")).ShouldBeNull();

    [Fact]
    public void An_unknown_frequency_is_off_and_fields_the_frequency_does_not_read_are_ignored()
    {
        Cadence("yearly").ShouldBe(ReportCadence.Off);
        Cadence("weekly", day: 15, weekday: "monday", time: "19:30", minute: 10)
            .ShouldBe(new ReportCadence(ReportFrequency.Weekly, 1, DayOfWeek.Monday, new TimeOnly(19, 30), 0));
        Cadence("monthly", day: 40, time: "25:00").ShouldBe(new ReportCadence(ReportFrequency.Monthly, 1, DayOfWeek.Sunday, new TimeOnly(20, 0), 0));
    }
}
