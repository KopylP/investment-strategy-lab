namespace InvestmentLab.Performance.Valuation.Domain;

public static class XirrCalculator
{
    public static decimal? Of(IReadOnlyList<(DateOnly Date, decimal Amount)> flows)
    {
        if (flows.Count < 2 || !flows.Any(flow => flow.Amount < 0m) || !flows.Any(flow => flow.Amount > 0m))
            return null;

        var start = flows.Min(flow => flow.Date);
        var points = flows.Select(flow => (Years: (flow.Date.DayNumber - start.DayNumber) / 365.0, Amount: (double)flow.Amount)).ToList();
        if (points.Max(point => point.Years) < 1.0 / 365)
            return null;

        double Npv(double rate) => points.Sum(point => point.Amount / Math.Pow(1 + rate, point.Years));

        double low = -0.99, high = 10.0;

        if (Npv(low) * Npv(high) > 0)
            return null;

        for (var i = 0; i < 200; i++)
        {
            var mid = (low + high) / 2;
            if (Npv(low) * Npv(mid) <= 0)
                high = mid;
            else
                low = mid;
        }

        return decimal.Round((decimal)((low + high) / 2), 6);
    }
}
