using System.Globalization;

namespace InvestmentLab.Signals.Monitoring.Application;

public sealed record SignalScheduleDto(string Frequency, int? Day, string? Weekday, string? Time, int? Minute)
{
    public static SignalScheduleDto Of(Cadence cadence) => new(
        cadence.Frequency.ToString(),
        cadence.Day,
        cadence.Weekday.ToString(),
        cadence.Time.ToString("HH:mm", CultureInfo.InvariantCulture),
        cadence.Minute);

    public Result<Cadence> ToCadence() => Cadence.TryOf(Frequency, Day, Weekday, Time, Minute);
}
