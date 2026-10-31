namespace InvestmentLab.Signals.Monitoring.Domain;

public static class SignalCheck
{
    public static SignalCheckOutcome Apply(Signal signal, IReadOnlyList<SignalOccurrence> open, SubjectReadings readings, DateTimeOffset now)
    {
        var openBySubject = open.ToDictionary(occurrence => occurrence.SubjectKey);

        var recovered = UpdateOpenAlarms(signal, openBySubject, readings, now);
        var gone = CloseGoneAlarms(open, readings, now);

        var triggerId = Guid.CreateVersion7();
        var fired = FireNewAlarms(signal, openBySubject, readings, triggerId, now);

        var stillActive = open.Count(occurrence => occurrence.IsOpen) + fired.Count;
        var firedTriggerId = fired.Count > 0 ? triggerId : (Guid?)null;

        return new SignalCheckOutcome(firedTriggerId, fired, recovered, gone, stillActive);
    }

    private static List<SignalOccurrence> UpdateOpenAlarms(
        Signal signal, Dictionary<string, SignalOccurrence> openBySubject, SubjectReadings readings, DateTimeOffset now)
    {
        var recovered = new List<SignalOccurrence>();

        foreach (var reading in readings.Measured)
        {
            if (!openBySubject.TryGetValue(reading.Key, out var occurrence))
                continue;

            occurrence.Update(reading);

            if (signal.Condition.IsRearmedBy(reading.Value))
            {
                occurrence.Clear(ClearReason.Recovered, now);
                recovered.Add(occurrence);
            }
        }

        return recovered;
    }

    private static List<SignalOccurrence> CloseGoneAlarms(IReadOnlyList<SignalOccurrence> open, SubjectReadings readings, DateTimeOffset now)
    {
        var gone = open.Where(occurrence => IsGone(occurrence, readings)).ToList();
        foreach (var occurrence in gone)
            occurrence.Clear(ClearReason.PositionClosed, now);

        return gone;
    }

    private static bool IsGone(SignalOccurrence occurrence, SubjectReadings readings) =>
        occurrence.IsOpen && !readings.LiveSubjects.Contains(occurrence.SubjectKey);

    private static List<FiredAlarm> FireNewAlarms(
        Signal signal, Dictionary<string, SignalOccurrence> openBySubject, SubjectReadings readings, Guid triggerId, DateTimeOffset now)
    {
        var crossing = readings.Measured
            .Where(reading => !openBySubject.ContainsKey(reading.Key))
            .Where(reading => signal.Condition.IsCrossedBy(reading.Value));

        return [.. crossing.Select(reading => new FiredAlarm(SignalOccurrence.Open(signal, triggerId, reading, now), reading))];
    }
}
