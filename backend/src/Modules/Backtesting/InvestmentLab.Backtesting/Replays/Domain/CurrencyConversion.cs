namespace InvestmentLab.Backtesting.Replays.Domain;

public static class CurrencyConversion
{
    public static IReadOnlyList<PricedDay> Converted(IReadOnlyList<PricedDay> prices, IReadOnlyList<DatedRate> rates)
    {
        var orderedRates = rates.OrderBy(rate => rate.Date).ToList();
        var converted = new List<PricedDay>(prices.Count);
        var known = -1;

        foreach (var price in prices.OrderBy(price => price.Date))
        {
            while (known + 1 < orderedRates.Count && orderedRates[known + 1].Date <= price.Date)
                known++;

            if (known >= 0)
                converted.Add(price with { Price = price.Price * orderedRates[known].Rate });
        }

        return converted;
    }
}
