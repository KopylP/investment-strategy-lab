namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class PriceTable
{
    private const int WarmupDays = 260;
    private const int LeastReplayedDays = 20;
    private const double ShareOfUniverseToStart = 0.5;

    private readonly Dictionary<Guid, decimal?[]> _prices = [];
    private readonly Dictionary<Guid, int[]> _observations = [];

    public IReadOnlyList<DateOnly> Days { get; }

    public PriceTable(IReadOnlyDictionary<Guid, IReadOnlyList<PricedDay>> history)
    {
        var days = history.Values.SelectMany(points => points.Select(point => point.Date)).Distinct().Order().ToList();

        Days = days;

        foreach (var (instrumentId, points) in history)
            Fill(instrumentId, points, days);
    }

    public IReadOnlyDictionary<Guid, decimal> PricesOn(int dayIndex) =>
        _prices
            .Where(entry => entry.Value[dayIndex] is not null)
            .ToDictionary(entry => entry.Key, entry => entry.Value[dayIndex]!.Value);

    public decimal? PriceOn(Guid instrumentId, int dayIndex) =>
        _prices.TryGetValue(instrumentId, out var prices) ? prices[dayIndex] : null;

    public int? FirstDayToReplay(ReplaySetup setup)
    {
        var needed = Math.Max(1, (int)Math.Round(setup.Universe.Count * ShareOfUniverseToStart));

        for (var dayIndex = 0; dayIndex < Days.Count - LeastReplayedDays; dayIndex++)
        {
            var warmed = setup.Universe.Count(instrumentId => HasWarmedUp(instrumentId, dayIndex));

            if (warmed >= needed && HasWarmedUp(setup.BenchmarkInstrumentId, dayIndex))
                return dayIndex;
        }

        return null;
    }

    private bool HasWarmedUp(Guid instrumentId, int dayIndex) =>
        _observations.TryGetValue(instrumentId, out var seen) && seen[dayIndex] >= WarmupDays;

    private void Fill(Guid instrumentId, IReadOnlyList<PricedDay> points, List<DateOnly> days)
    {
        var byDay = points.ToDictionary(point => point.Date, point => point.Price);
        var prices = new decimal?[days.Count];
        var seen = new int[days.Count];

        decimal? last = null;
        var count = 0;

        for (var dayIndex = 0; dayIndex < days.Count; dayIndex++)
        {
            if (byDay.TryGetValue(days[dayIndex], out var price))
            {
                last = price;
                count++;
            }

            prices[dayIndex] = last;
            seen[dayIndex] = count;
        }

        _prices[instrumentId] = prices;
        _observations[instrumentId] = seen;
    }
}
