namespace InvestmentLab.Signals.Monitoring.Domain;

public static class SignalMetrics
{
    public static decimal? DrawdownFromPeak(IReadOnlyList<decimal> values)
    {
        if (values.Count == 0)
            return null;

        var peak = values.Max();

        return peak > 0m ? 1m - values[^1] / peak : null;
    }

    public static decimal? DayFall(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2 || values[^2] <= 0m)
            return null;

        return 1m - values[^1] / values[^2];
    }

    public static decimal? LossFromCost(decimal price, decimal averageCost) =>
        averageCost > 0m ? 1m - price / averageCost : null;
}
