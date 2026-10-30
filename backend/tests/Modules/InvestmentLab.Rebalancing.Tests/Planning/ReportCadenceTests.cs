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
    [MemberData(nameof(Times), DisableDiscoveryEnumeration = true)]  // ReportCadence is not serializable
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

    [Theory]
    [InlineData("monthly", null, null, "09:00", null, "report_schedule.day")]
    [InlineData("monthly", 32, null, "09:00", null, "report_schedule.day")]
    [InlineData("weekly", null, "sun", "09:00", null, "report_schedule.weekday")]
    [InlineData("daily", null, null, "9:00", null, "report_schedule.time")]
    [InlineData("hourly", null, null, null, 60, "report_schedule.minute")]
    [InlineData("yearly", null, null, null, null, "report_schedule.frequency")]
    [InlineData("3", null, null, null, null, "report_schedule.frequency")]
    public void The_users_own_schedule_needs_the_fields_its_frequency_reads(string frequency, int? day, string? weekday, string? time, int? minute, string error) =>
        ReportCadence.TryOf(frequency, day, weekday, time, minute).Error.Code.ShouldBe(error);

    [Fact]
    public void The_users_own_schedule_keeps_only_what_its_frequency_reads()
    {
        ReportCadence.TryOf("monthly", 10, "friday", "09:00", 30).Value
            .ShouldBe(new ReportCadence(ReportFrequency.Monthly, 10, DayOfWeek.Sunday, new TimeOnly(9, 0), 0));
        ReportCadence.TryOf("off", null, null, null, null).Value.ShouldBe(ReportCadence.Off);
    }

    [Fact]
    public void An_accounts_own_schedule_replaces_the_strategys_until_reset()
    {
        var settings = PlanSettings.DefaultFor(Guid.NewGuid(), UserId.Of(Guid.NewGuid()));
        var payday = ReportCadence.TryOf("monthly", 10, null, "09:00", null).Value;

        var setAt = new DateTimeOffset(2026, 10, 30, 19, 43, 0, TimeSpan.Zero);
        settings.Schedule("rebalance", Cadence("weekly"), setAt.AddDays(-1));
        settings.Schedule("rebalance", payday, setAt);
        settings.ScheduleOf("rebalance").ShouldBe(new OwnReportSchedule("rebalance", payday, setAt));
        settings.ScheduleOf("risk-review").ShouldBeNull();
        settings.ReportSchedules.Count.ShouldBe(1);

        // Stored as jsonb and read back the same.
        var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        System.Text.Json.JsonSerializer.Deserialize<List<OwnReportSchedule>>(System.Text.Json.JsonSerializer.Serialize(settings.ReportSchedules, options), options)
            .ShouldBe(settings.ReportSchedules);

        settings.UseStrategySchedule("rebalance", DateTimeOffset.UtcNow);
        settings.ScheduleOf("rebalance").ShouldBeNull();
    }
}
