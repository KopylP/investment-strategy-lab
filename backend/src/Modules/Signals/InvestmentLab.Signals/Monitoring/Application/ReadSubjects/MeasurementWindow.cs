using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

internal static class MeasurementWindow
{
    private const int WholeHistoryDays = 365 * 30;
    private const int RecentTradingDays = 14;

    public static DateOnly StartOf(SignalCondition condition, DateOnly today)
    {
        var days = condition.Metric == SignalMetric.DrawdownFromPeak
            ? condition.WindowDays ?? WholeHistoryDays
            : RecentTradingDays;

        return today.AddDays(-days);
    }
}
