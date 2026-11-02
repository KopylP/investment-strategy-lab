namespace InvestmentLab.Signals.Monitoring.Domain;

public static class MeasurementWindow
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

    public static DateOnly StartOfHolding(SignalCondition condition, DateOnly today, DateOnly heldSince)
    {
        var windowStart = StartOf(condition, today);

        if (condition.Metric != SignalMetric.DrawdownFromPeak)
            return windowStart;

        return windowStart > heldSince ? windowStart : heldSince;
    }
}
