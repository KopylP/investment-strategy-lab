namespace InvestmentLab.MarketData.Quotes.Domain;

/// <summary>Monday to Friday; exchange holidays are not modelled.</summary>
public static class BusinessDays
{
    public static bool IsBusinessDay(DateOnly day) => day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    /// <summary>Business days after <paramref name="from"/> up to and including <paramref name="to"/>.</summary>
    public static int CountAfter(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var day = from.AddDays(1); day <= to; day = day.AddDays(1))
        {
            if (IsBusinessDay(day))
                count++;
        }

        return count;
    }
}
