namespace InvestmentLab.BuildingBlocks.Scheduling;

public sealed record IntervalSchedule(TimeSpan Every) : IJobSchedule
{
    public DateTimeOffset NextOccurrenceAfter(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        var ticks = (utc.Ticks / Every.Ticks + 1) * Every.Ticks;

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
