namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed class SignalOccurrence : AggregateRoot<Guid>
{
    public Guid SignalId { get; private set; }
    public UserId OwnerId { get; private set; }
    public Guid TriggerId { get; private set; }
    public string SubjectKey { get; private set; }
    public string SubjectLabel { get; private set; }
    public decimal TriggerValue { get; private set; }
    public DateOnly TriggerDate { get; private set; }
    public DateTimeOffset TriggeredAt { get; private set; }
    public decimal LatestValue { get; private set; }
    public DateOnly LatestDate { get; private set; }
    public DateTimeOffset? ClearedAt { get; private set; }
    public ClearReason? ClearReason { get; private set; }

    public bool IsOpen => ClearedAt is null;

    private SignalOccurrence(Guid id) : base(id) => (SubjectKey, SubjectLabel) = (null!, null!);

    private SignalOccurrence(Signal signal, Guid triggerId, SubjectReading reading, DateTimeOffset now) : base(Guid.CreateVersion7())
    {
        SignalId = signal.Id;
        OwnerId = signal.OwnerId;
        TriggerId = triggerId;
        SubjectKey = reading.Key;
        SubjectLabel = reading.Label;
        TriggerValue = reading.Value;
        TriggerDate = reading.DataDate;
        TriggeredAt = now;
        LatestValue = reading.Value;
        LatestDate = reading.DataDate;
    }

    public static SignalOccurrence Open(Signal signal, Guid triggerId, SubjectReading reading, DateTimeOffset now) =>
        new(signal, triggerId, reading, now);

    public void Update(SubjectReading reading)
    {
        SubjectLabel = reading.Label;
        LatestValue = reading.Value;
        LatestDate = reading.DataDate;
    }

    public void Clear(ClearReason reason, DateTimeOffset now)
    {
        if (!IsOpen)
            return;

        ClearedAt = now;
        ClearReason = reason;
    }
}
