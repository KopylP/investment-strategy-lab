using InvestmentLab.BuildingBlocks.Scheduling;

namespace InvestmentLab.Rebalancing.Planning.Application;

public static class ReportSchedules
{
    public const string TimeZone = "Europe/Warsaw";

    public static readonly IntervalSchedule Check = new(TimeSpan.FromMinutes(1));

    public static readonly IntervalSchedule Requests = new(TimeSpan.FromSeconds(5));

    public static readonly TimeSpan CatchUp = TimeSpan.FromDays(1);

    public static DateTimeOffset Local(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(TimeZone));
}
