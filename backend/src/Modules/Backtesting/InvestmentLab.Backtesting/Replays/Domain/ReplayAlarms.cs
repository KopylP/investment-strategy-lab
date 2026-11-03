namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayAlarms(ReplaySignalTriggers triggers, IReadOnlyList<Guid> logicSignalIds, ReplaySignalWatches watches)
{
    private const string AccountKey = "account";
    private const string VolatilityIndexKey = "volatility-index";

    public FiredReports FiredOn(ReplayMoment moment)
    {
        var firedKinds = KindsFiredOn(moment);
        var firedSignals = SignalsFiredOn(moment);
        var states = logicSignalIds.Select(id => new ReplaySignalState(id, watches.User[id].ActiveInstruments)).ToList();

        return new FiredReports(
            Rebalance: triggers.RebalanceKinds.Any(firedKinds.Contains) || (triggers.RebalanceSignalIds ?? []).Any(firedSignals.Contains),
            RiskReview: triggers.RiskReviewKinds.Any(firedKinds.Contains) || (triggers.RiskReviewSignalIds ?? []).Any(firedSignals.Contains),
            States: states.Count == 0 ? null : states);
    }

    private HashSet<string> KindsFiredOn(ReplayMoment moment)
    {
        var fired = new HashSet<string>();

        foreach (var (kind, watch) in watches.BuiltIn)
        {
            if (watch.FiresOn(moment.Day, ReadingsOf(watch, moment)))
                fired.Add(kind);
        }

        return fired;
    }

    private HashSet<Guid> SignalsFiredOn(ReplayMoment moment)
    {
        var fired = new HashSet<Guid>();

        foreach (var (id, watch) in watches.User)
        {
            if (watch.FiresOn(moment.Day, ReadingsOf(watch, moment)))
                fired.Add(id);
        }

        return fired;
    }

    private List<ReplaySignalReading> ReadingsOf(IReplaySignalWatch watch, ReplayMoment moment) => watch.Subject switch
    {
        ReplaySignalSubject.Instruments => InstrumentReadings(watch.Instruments, moment),
        ReplaySignalSubject.VolatilityIndex => VolatilityIndexReadings(moment),
        ReplaySignalSubject.Account => [new ReplaySignalReading(AccountKey, (decimal)moment.Growth)],
        ReplaySignalSubject.EachPosition => PositionReadings(moment),
        _ => throw new ArgumentOutOfRangeException(nameof(watch), watch.Subject, "Unknown subject of a replayed signal."),
    };

    private static List<ReplaySignalReading> InstrumentReadings(IReadOnlyList<Guid> instruments, ReplayMoment moment)
    {
        var priced = instruments.Where(moment.Prices.ContainsKey);

        return [.. priced.Select(id => new ReplaySignalReading(id.ToString("N"), moment.Prices[id]))];
    }

    private List<ReplaySignalReading> VolatilityIndexReadings(ReplayMoment moment)
    {
        if (triggers.VolatilityIndexInstrumentId is not { } instrumentId || !moment.Prices.TryGetValue(instrumentId, out var level))
            return [];

        return [new ReplaySignalReading(VolatilityIndexKey, level)];
    }

    private static List<ReplaySignalReading> PositionReadings(ReplayMoment moment)
    {
        var priced = moment.Book.Positions.Where(position => moment.Prices.ContainsKey(position.InstrumentId));

        return [.. priced.Select(position => new ReplaySignalReading(
            SubjectKey: position.InstrumentId.ToString("N"),
            Value: moment.Prices[position.InstrumentId],
            HeldSince: position.FirstAcquiredOn,
            UnitCost: position.AverageCost))];
    }
}
