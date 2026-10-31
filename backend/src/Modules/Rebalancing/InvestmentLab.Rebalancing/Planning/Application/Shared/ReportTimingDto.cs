using System.Globalization;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record ReportTimingDto(string Frequency, int Day, string Weekday, string Time, int Minute)
{
    public static ReportTimingDto Of(Cadence cadence) => new(
        cadence.Frequency.ToString(),
        cadence.Day,
        cadence.Weekday.ToString(),
        cadence.Time.ToString("HH:mm", CultureInfo.InvariantCulture),
        cadence.Minute);
}
