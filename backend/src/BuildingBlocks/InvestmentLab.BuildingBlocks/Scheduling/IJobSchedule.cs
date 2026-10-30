namespace InvestmentLab.BuildingBlocks.Scheduling;

/// <summary>When a scheduled job runs.</summary>
public interface IJobSchedule
{
    /// <summary>The first occurrence strictly after <paramref name="instant"/>.</summary>
    DateTimeOffset NextOccurrenceAfter(DateTimeOffset instant);
}

/// <summary>Every <paramref name="Every"/> (at whole multiples of it since midnight UTC): for a job that decides by itself whether anything is due.</summary>
public sealed record IntervalSchedule(TimeSpan Every) : IJobSchedule
{
    public DateTimeOffset NextOccurrenceAfter(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        var ticks = (utc.Ticks / Every.Ticks + 1) * Every.Ticks;
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
