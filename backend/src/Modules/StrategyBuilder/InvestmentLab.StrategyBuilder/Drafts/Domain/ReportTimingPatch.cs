namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record ReportTimingPatch(string Frequency, int? Day, string? Weekday, string? Time, int? Minute)
{
    public Result<ReportTiming> Over(ReportTiming current)
    {
        var day = Day ?? current.Day;
        var weekday = Weekday ?? current.Weekday;
        var time = Time ?? current.Time;
        var minute = Minute ?? current.Minute;

        var cadence = Cadence.TryOf(Frequency, day, weekday, time, minute);
        if (cadence.IsFailure)
            return cadence.Error;

        return new ReportTiming(Frequency.ToLowerInvariant(), day, weekday, time, minute);
    }
}
