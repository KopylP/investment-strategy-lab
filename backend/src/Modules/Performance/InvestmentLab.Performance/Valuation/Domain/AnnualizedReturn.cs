namespace InvestmentLab.Performance.Valuation.Domain;

public static class AnnualizedReturn
{
    private const decimal DaysInYear = 365.25m;
    private const decimal ShortestAnnualizedYears = 1m;

    public static decimal? Of(decimal totalReturn, DateOnly start, DateOnly end)
    {
        var years = (end.DayNumber - start.DayNumber) / DaysInYear;
        if (years < ShortestAnnualizedYears)
            return null;

        var growth = 1m + totalReturn;
        if (growth < 0m)
            return null;

        var annualGrowth = Math.Pow((double)growth, (double)(1m / years));

        return (decimal)annualGrowth - 1m;
    }
}
