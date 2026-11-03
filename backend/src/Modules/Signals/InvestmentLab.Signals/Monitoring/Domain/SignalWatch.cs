namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed class SignalWatch(SignalCondition condition)
{
    private readonly Dictionary<string, List<DatedValue>> _history = [];
    private readonly HashSet<string> _alarmed = [];

    public IReadOnlyCollection<string> Alarmed => [.. _alarmed];

    public bool FiresOn(DateOnly day, IReadOnlyList<WatchedReading> readings)
    {
        ForgetSubjectsOtherThan(readings);

        var fired = false;

        foreach (var reading in readings)
        {
            Remember(reading, day);

            if (MeasureOf(reading, day) is { } measured)
                fired |= Opens(reading.SubjectKey, measured);
        }

        return fired;
    }

    private void ForgetSubjectsOtherThan(IReadOnlyList<WatchedReading> readings)
    {
        var live = readings.Select(reading => reading.SubjectKey).ToHashSet();

        _alarmed.RemoveWhere(subject => !live.Contains(subject));
    }

    private void Remember(WatchedReading reading, DateOnly day)
    {
        if (!_history.TryGetValue(reading.SubjectKey, out var values))
            _history[reading.SubjectKey] = values = [];

        values.Add(new DatedValue(day, reading.Value));
    }

    private decimal? MeasureOf(WatchedReading reading, DateOnly day) => condition.Metric switch
    {
        SignalMetric.Level => reading.Value,
        SignalMetric.DrawdownFromPeak => SignalMetrics.DrawdownFromPeak(ValuesInWindow(reading, day)),
        SignalMetric.DayFall => SignalMetrics.DayFall(ValuesInWindow(reading, day)),
        SignalMetric.LossFromCost => reading.UnitCost is { } unitCost ? SignalMetrics.LossFromCost(reading.Value, unitCost) : null,
        _ => throw new NotSupportedException($"A {condition.Metric} signal is not watched over a replayed history."),
    };

    private List<decimal> ValuesInWindow(WatchedReading reading, DateOnly day)
    {
        var start = reading.HeldSince is { } heldSince
            ? MeasurementWindow.StartOfHolding(condition, day, heldSince)
            : MeasurementWindow.StartOf(condition, day);

        return [.. _history[reading.SubjectKey].Where(value => value.Date >= start).Select(value => value.Value)];
    }

    private bool Opens(string subjectKey, decimal measured)
    {
        if (_alarmed.Contains(subjectKey))
        {
            if (condition.IsRearmedBy(measured))
                _alarmed.Remove(subjectKey);

            return false;
        }

        if (!condition.IsCrossedBy(measured))
            return false;

        _alarmed.Add(subjectKey);

        return true;
    }

    private sealed record DatedValue(DateOnly Date, decimal Value);
}
