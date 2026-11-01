namespace InvestmentLab.MarketData.FxRates.Domain;

public static class CrossRate
{
    public static decimal? Of(decimal? fromInPivot, decimal? toInPivot) =>
        fromInPivot is { } from && toInPivot is { } to && to > 0m ? from / to : null;

    public static IReadOnlyList<(DateOnly Date, decimal Rate)> SeriesOf(IReadOnlyList<(DateOnly Date, decimal Rate)>? fromLeg, IReadOnlyList<(DateOnly Date, decimal Rate)>? toLeg)
    {
        var days = DaysOf(fromLeg).Union(DaysOf(toLeg)).Order();
        var series = new List<(DateOnly Date, decimal Rate)>();

        foreach (var day in days)
        {
            if (Of(LegOn(fromLeg, day), LegOn(toLeg, day)) is { } rate)
                series.Add((day, rate));
        }

        return series;
    }

    private static IEnumerable<DateOnly> DaysOf(IReadOnlyList<(DateOnly Date, decimal Rate)>? leg) =>
        leg?.Select(point => point.Date) ?? [];

    private static decimal? LegOn(IReadOnlyList<(DateOnly Date, decimal Rate)>? leg, DateOnly day)
    {
        if (leg is null)
            return 1m;

        var known = leg.Where(point => point.Date <= day).ToList();

        return known.Count == 0 ? null : known.MaxBy(point => point.Date).Rate;
    }
}
