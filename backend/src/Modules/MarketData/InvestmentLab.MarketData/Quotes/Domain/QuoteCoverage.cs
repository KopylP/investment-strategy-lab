namespace InvestmentLab.MarketData.Quotes.Domain;

public sealed record QuoteCoverage(DateOnly? First, DateOnly? Last, int Count, int LongestGapDays)
{
    public static QuoteCoverage Of(IReadOnlyList<DateOnly> quotedDays)
    {
        var days = quotedDays.Distinct().Order().ToList();
        if (days.Count == 0)
            return new QuoteCoverage(null, null, 0, 0);

        return new QuoteCoverage(days[0], days[^1], days.Count, LongestGapOf(days));
    }

    private static int LongestGapOf(IReadOnlyList<DateOnly> days)
    {
        var gaps = days.Skip(1).Select((day, index) => day.DayNumber - days[index].DayNumber);

        return gaps.DefaultIfEmpty(0).Max();
    }
}
