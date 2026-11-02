namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayAlarms(ReplaySignalTriggers triggers, IReplaySignals signals)
{
    private const string AccountKey = "account";
    private const string VolatilityIndexKey = "volatility-index";

    private readonly ReplaySignalTriggers _triggers = triggers;
    private readonly Dictionary<string, IReplaySignalWatch> _watches = triggers.AllKinds.ToDictionary(kind => kind, signals.Watch);

    public FiredReports FiredOn(ReplayMoment moment)
    {
        var firedKinds = KindsFiredOn(moment);

        return new FiredReports(
            Rebalance: _triggers.RebalanceKinds.Any(firedKinds.Contains),
            RiskReview: _triggers.RiskReviewKinds.Any(firedKinds.Contains));
    }

    private HashSet<string> KindsFiredOn(ReplayMoment moment)
    {
        var fired = new HashSet<string>();

        foreach (var (kind, watch) in _watches)
        {
            if (watch.FiresOn(moment.Day, ReadingsOf(watch.Subject, moment)))
                fired.Add(kind);
        }

        return fired;
    }

    private List<ReplaySignalReading> ReadingsOf(ReplaySignalSubject subject, ReplayMoment moment) => subject switch
    {
        ReplaySignalSubject.VolatilityIndex => VolatilityIndexReadings(moment),
        ReplaySignalSubject.Account => [new ReplaySignalReading(AccountKey, (decimal)moment.Growth)],
        ReplaySignalSubject.EachPosition => PositionReadings(moment),
        _ => throw new ArgumentOutOfRangeException(nameof(subject), subject, "Unknown subject of a replayed signal."),
    };

    private List<ReplaySignalReading> VolatilityIndexReadings(ReplayMoment moment)
    {
        if (_triggers.VolatilityIndexInstrumentId is not { } instrumentId || !moment.Prices.TryGetValue(instrumentId, out var level))
            return [];

        return [new ReplaySignalReading(VolatilityIndexKey, level)];
    }

    private static List<ReplaySignalReading> PositionReadings(ReplayMoment moment)
    {
        var priced = moment.Book.Positions.Where(position => moment.Prices.ContainsKey(position.InstrumentId));

        return [.. priced.Select(position => new ReplaySignalReading(
            SubjectKey: position.InstrumentId.ToString("N"),
            Value: moment.Prices[position.InstrumentId],
            HeldSince: position.FirstAcquiredOn))];
    }
}
