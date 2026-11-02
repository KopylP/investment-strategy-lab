namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayComparisons(PriceTable prices, int firstDay, ReplaySetup setup)
{
    public static readonly Guid EqualWeightBasket = new("5e1f0c3a-7d2b-4b6e-9a51-0e4c8f2d9b17");

    public IReadOnlyList<GrowthDay> Benchmark => GrowthOf(setup.BenchmarkInstrumentId);

    public double RiskFreeRate =>
        setup.CashInstrumentId is { } cash && GrowthOf(cash) is { Count: > 1 } growth ? ReplaySeries.AnnualReturnOf(growth) : 0d;

    public IReadOnlyList<GrowthDay> EqualWeight()
    {
        var growth = new List<GrowthDay>();
        var value = 1d;

        for (var dayIndex = firstDay; dayIndex < prices.Days.Count; dayIndex++)
        {
            value *= 1d + AverageReturnOn(dayIndex);
            growth.Add(new GrowthDay(prices.Days[dayIndex], value));
        }

        return growth;
    }

    private double AverageReturnOn(int dayIndex)
    {
        if (dayIndex == firstDay)
            return 0d;

        var returns = setup.Universe.Select(instrumentId => ReturnOn(instrumentId, dayIndex)).OfType<double>().ToList();

        return returns.Count > 0 ? returns.Average() : 0d;
    }

    private double? ReturnOn(Guid instrumentId, int dayIndex) =>
        prices.PriceOn(instrumentId, dayIndex) is { } today && prices.PriceOn(instrumentId, dayIndex - 1) is { } before && before > 0m
            ? (double)(today / before) - 1d
            : null;

    private List<GrowthDay> GrowthOf(Guid instrumentId)
    {
        var days = Enumerable.Range(firstDay, prices.Days.Count - firstDay)
            .Select(dayIndex => (Index: dayIndex, Price: prices.PriceOn(instrumentId, dayIndex)))
            .Where(day => day.Price is not null)
            .ToList();

        return [.. days.Select(day => new GrowthDay(prices.Days[day.Index], (double)(day.Price!.Value / days[0].Price!.Value)))];
    }
}
