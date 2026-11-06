namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public static class ValuationPrices
{
    // Preserve signal prices. Monetary valuations use only FX rates already published on that day.
    public static IReadOnlyList<MarketPoint>? Converted(IReadOnlyList<MarketPoint> points, IReadOnlyList<(DateOnly Date, decimal Rate)> rates)
    {
        var ordered = rates.OrderBy(rate => rate.Date).ToList();
        var result = new List<MarketPoint>();
        var index = -1;
        foreach (var point in points.OrderBy(point => point.Date))
        {
            while (index + 1 < ordered.Count && ordered[index + 1].Date <= point.Date)
                index++;
            if (index < 0 || ordered[index].Rate <= 0m || point.Date.DayNumber - ordered[index].Date.DayNumber > 7)
                return null;
            result.Add(point with { ValuationClose = point.AdjustedClose * ordered[index].Rate });
        }
        return result;
    }
}
