namespace InvestmentLab.Performance.Valuation.Domain;

public static class DrawdownCalculator
{
    public sealed record Drawdown(decimal Max, decimal Current, DateOnly? PeakDate, DateOnly? TroughDate);

    public static Drawdown Of(IReadOnlyList<(DateOnly Date, decimal Index)> series)
    {
        decimal peak = 0m, max = 0m, current = 0m;
        DateOnly? peakDate = null, troughDate = null, maxPeakDate = null;

        foreach (var (date, index) in series)
        {
            if (index > peak)
                (peak, peakDate) = (index, date);

            current = peak > 0m ? index / peak - 1m : 0m;

            if (current < max)
                (max, troughDate, maxPeakDate) = (current, date, peakDate);
        }

        return new Drawdown(decimal.Round(max, 6), decimal.Round(current, 6), maxPeakDate, troughDate);
    }
}
