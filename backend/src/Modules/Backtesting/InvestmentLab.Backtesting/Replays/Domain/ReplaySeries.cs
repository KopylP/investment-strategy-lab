namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySeries(ReplaySeriesKind Kind, ReplaySummary Summary, IReadOnlyList<ReplayPoint> Points)
{
    private const double DaysInYear = 365.25;
    private const double TradingDaysInYear = 252d;

    public static ReplaySeries Of(ReplaySeriesKind kind, IReadOnlyList<GrowthDay> days, double riskFreeRate) =>
        new(kind, SummaryOf(days, riskFreeRate), MonthlyPointsOf(days));

    public static double AnnualReturnOf(IReadOnlyList<GrowthDay> days)
    {
        var years = Math.Max((days[^1].Date.DayNumber - days[0].Date.DayNumber) / DaysInYear, 1e-9);

        return Math.Pow(days[^1].Growth / days[0].Growth, 1d / years) - 1d;
    }

    private static ReplaySummary SummaryOf(IReadOnlyList<GrowthDay> days, double riskFreeRate)
    {
        var annualReturn = AnnualReturnOf(days);
        var volatility = VolatilityOf(days);
        var sharpe = volatility > 0d ? (annualReturn - riskFreeRate) / volatility : 0d;

        return new ReplaySummary(annualReturn, volatility, sharpe, DrawdownsOf(days).Min());
    }

    private static double VolatilityOf(IReadOnlyList<GrowthDay> days)
    {
        var returns = days.Skip(1).Select((day, index) => day.Growth / days[index].Growth - 1d).ToList();
        if (returns.Count < 2)
            return 0d;

        var mean = returns.Average();
        var variance = returns.Sum(value => (value - mean) * (value - mean)) / (returns.Count - 1);

        return Math.Sqrt(variance * TradingDaysInYear);
    }

    private static List<double> DrawdownsOf(IReadOnlyList<GrowthDay> days)
    {
        var drawdowns = new List<double>(days.Count);
        var peak = double.MinValue;

        foreach (var day in days)
        {
            peak = Math.Max(peak, day.Growth);
            drawdowns.Add(day.Growth / peak - 1d);
        }

        return drawdowns;
    }

    private static List<ReplayPoint> MonthlyPointsOf(IReadOnlyList<GrowthDay> days)
    {
        var drawdowns = DrawdownsOf(days);
        var months = days.Select((day, index) => (Day: day, Drawdown: drawdowns[index])).GroupBy(entry => (entry.Day.Date.Year, entry.Day.Date.Month));

        return [.. months.Select(month => new ReplayPoint(month.Last().Day.Date, month.Last().Day.Growth, month.Min(entry => entry.Drawdown)))];
    }
}
