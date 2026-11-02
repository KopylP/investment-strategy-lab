namespace InvestmentLab.Backtesting.Replays.Domain;

public static class AfterTaxGrowth
{
    public static double FinalOf(IReadOnlyList<GrowthDay> days, IReadOnlyList<YearTax> taxes, decimal capital)
    {
        var dueByYear = taxes.ToDictionary(tax => tax.Year, tax => (double)tax.Amount);
        var lastYear = days[^1].Date.Year;
        var keptShare = 1d;

        for (var index = 1; index < days.Count; index++)
        {
            var yearClosed = days[index - 1].Date.Year;
            if (days[index].Date.Year != yearClosed && dueByYear.TryGetValue(yearClosed, out var due))
                keptShare *= ShareLeftAfterPaying(due, days[index], capital);
        }

        if (dueByYear.TryGetValue(lastYear, out var dueAtEnd))
            keptShare *= ShareLeftAfterPaying(dueAtEnd, days[^1], capital);

        return days[^1].Growth * keptShare;
    }

    private static double ShareLeftAfterPaying(double due, GrowthDay day, decimal capital)
    {
        var value = day.Growth * (double)capital;

        return value > 0d ? Math.Max(0d, 1d - due / value) : 0d;
    }
}
