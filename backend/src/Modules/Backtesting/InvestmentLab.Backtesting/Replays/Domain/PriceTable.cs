namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class PriceTable
{
    private const int LeastReplayedDays = 20;
    private const double ShareOfUniverseToStart = 0.5;

    private readonly Dictionary<Guid, decimal?[]> _prices = [];
    private readonly Dictionary<Guid, int[]> _observations = [];
    private readonly Dictionary<Guid, HashSet<DateOnly>> _quotedDays = [];

    public IReadOnlyList<DateOnly> Days { get; }

    public PriceTable ForReplay(ReplaySetup setup) => setup.Experiment is { } experiment ? Until(experiment.End) : this;

    public PriceTable Until(DateOnly end)
    {
        var indices = Days.Select((day, index) => (day, index)).ToDictionary(entry => entry.day, entry => entry.index);
        return new(_quotedDays.ToDictionary(entry => entry.Key,
            entry => (IReadOnlyList<PricedDay>)[.. entry.Value.Where(day => day <= end).Order()
                .Select(day => new PricedDay(day, PriceOn(entry.Key, indices[day])!.Value))]));
    }

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

    public IReadOnlyDictionary<Guid, decimal> TradablePricesOn(int dayIndex) =>
        PricesOn(dayIndex).Where(entry => _quotedDays[entry.Key].Contains(Days[dayIndex]))
            .ToDictionary(entry => entry.Key, entry => entry.Value);

    public decimal? PriceOn(Guid instrumentId, int dayIndex) =>
        _prices.TryGetValue(instrumentId, out var prices) ? prices[dayIndex] : null;

    public double? ReturnOn(Guid instrumentId, int dayIndex)
    {
        if (dayIndex == 0 || PriceOn(instrumentId, dayIndex) is not { } today || PriceOn(instrumentId, dayIndex - 1) is not { } before)
            return null;

        return before > 0m ? (double)(today / before) - 1d : null;
    }

    public List<GrowthDay> GrowthOf(Guid instrumentId, int firstDay)
    {
        var priced = Enumerable.Range(firstDay, Days.Count - firstDay)
            .Select(dayIndex => (Index: dayIndex, Price: PriceOn(instrumentId, dayIndex)))
            .Where(day => day.Price is not null)
            .ToList();

        if (priced.Count == 0)
            return [];

        var start = priced[0].Price!.Value;

        return [.. priced.Select(day => new GrowthDay(Days[day.Index], (double)(day.Price!.Value / start)))];
    }

    public int? FirstDayToReplay(ReplaySetup setup)
    {
        var from = setup.Experiment?.Start ?? DateOnly.MinValue;

        return FirstReadyDayFrom(setup, from);
    }

    public HistoryShortfall? ShortfallAt(ReplaySetup setup, DateOnly requestedStart)
    {
        var earliest = FirstReadyDayFrom(setup, DateOnly.MinValue);
        if (earliest is null)
            return null;

        var requestedDay = FirstDayOnOrAfter(requestedStart) ?? Days.Count - 1;
        var limit = LimitOn(setup, requestedDay);

        return new HistoryShortfall(Days[earliest.Value], limit);
    }

    private int? FirstReadyDayFrom(ReplaySetup setup, DateOnly from)
    {
        for (var dayIndex = 0; dayIndex < Days.Count - LeastReplayedDays; dayIndex++)
        {
            if (Days[dayIndex] >= from && IsReadyOn(setup, dayIndex))
                return dayIndex;
        }

        return null;
    }

    private bool IsReadyOn(ReplaySetup setup, int dayIndex)
    {
        var warmed = setup.Universe.Count(instrumentId => HasWarmedUp(instrumentId, dayIndex, setup.HistoryBars));
        var referencesWarmed = ReferencesOf(setup).All(id => HasWarmedUp(id, dayIndex, setup.HistoryBars));

        return warmed >= FundsNeededToStart(setup) && referencesWarmed;
    }

    private HistoryLimit LimitOn(ReplaySetup setup, int dayIndex)
    {
        var coldReferences = ReferencesOf(setup).Where(id => !HasWarmedUp(id, dayIndex, setup.HistoryBars)).ToList();
        if (coldReferences.Count > 0)
            return LatestWarmingOf(coldReferences, setup.HistoryBars);

        var coldFunds = setup.Universe.Where(id => !HasWarmedUp(id, dayIndex, setup.HistoryBars)).ToList();
        var warmed = setup.Universe.Count - coldFunds.Count;
        var needed = FundsNeededToStart(setup);
        if (warmed < needed)
            return new FundsHistoryLimit(warmed, needed, coldFunds);

        return new QuotesHistoryLimit(Days[dayIndex]);
    }

    private ReferenceHistoryLimit LatestWarmingOf(IReadOnlyList<Guid> references, int historyBars)
    {
        var warming = references
            .Select(id => (Id: id, From: WarmedFrom(id, historyBars) ?? DateOnly.MaxValue))
            .MaxBy(reference => reference.From);

        return new ReferenceHistoryLimit(warming.Id, warming.From);
    }

    private DateOnly? WarmedFrom(Guid instrumentId, int historyBars)
    {
        var dayIndex = Enumerable.Range(0, Days.Count).FirstOrDefault(index => HasWarmedUp(instrumentId, index, historyBars), -1);

        return dayIndex >= 0 ? Days[dayIndex] : null;
    }

    private int? FirstDayOnOrAfter(DateOnly day)
    {
        var dayIndex = Enumerable.Range(0, Days.Count).FirstOrDefault(index => Days[index] >= day, -1);

        return dayIndex >= 0 ? dayIndex : null;
    }

    private static IEnumerable<Guid> ReferencesOf(ReplaySetup setup) =>
        (setup.ExtraSeries ?? []).Append(setup.Benchmark.InstrumentId);

    private static int FundsNeededToStart(ReplaySetup setup) =>
        Math.Max(1, (int)Math.Round(setup.Universe.Count * ShareOfUniverseToStart));

    private bool HasWarmedUp(Guid instrumentId, int dayIndex, int historyBars) =>
        _observations.TryGetValue(instrumentId, out var seen) && seen[dayIndex] >= historyBars;

    private void Fill(Guid instrumentId, IReadOnlyList<PricedDay> points, List<DateOnly> days)
    {
        var byDay = points.ToDictionary(point => point.Date, point => point.Price);
        _quotedDays[instrumentId] = [.. byDay.Keys];
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
