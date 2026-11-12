namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed class Signal : AggregateRoot<Guid>, IUserOwned
{
    public const int MaxNameLength = 100;

    private static readonly TimeSpan DataPollInterval = TimeSpan.FromMinutes(10);

    public UserId OwnerId { get; private set; }
    public string Name { get; private set; }
    public SignalTarget Target { get; private set; }
    public SignalCondition Condition { get; private set; }
    public Cadence Schedule { get; private set; }
    public bool Enabled { get; private set; }
    public string? BuiltInKey { get; private set; }
    public DateTimeOffset? LastCheckedAt { get; private set; }

    public bool IsBuiltIn => BuiltInKey is not null;

    private Signal(Guid id) : base(id) => (Name, Target, Condition, Schedule) = (null!, null!, null!, null!);

    private Signal(UserId ownerId, string name, SignalTarget target, SignalCondition condition, Cadence schedule, string? builtInKey)
        : base(Guid.CreateVersion7())
    {
        OwnerId = ownerId;
        Name = name.Trim();
        Target = target;
        Condition = condition;
        Schedule = schedule;
        BuiltInKey = builtInKey;
        Enabled = true;
    }

    public static Result<Signal> Create(
        UserId ownerId, string name, SignalTarget target, SignalCondition condition, Cadence schedule, string? builtInKey = null)
    {
        if (ValidationError(name, target, condition) is { } error)
            return error;

        return new Signal(ownerId, name, target, condition, schedule, builtInKey);
    }

    public Result Change(SignalSettings settings, IReadOnlyList<SignalOccurrence> openAlarms, DateTimeOffset now)
    {
        if (ValidationError(settings.Name, Target, settings.Condition) is { } error)
            return error;

        var closeReason = ReasonToCloseAlarms(settings);

        Name = settings.Name.Trim();
        Condition = settings.Condition;
        Schedule = settings.Schedule;
        Enabled = settings.Enabled;
        LastCheckedAt = null;

        if (closeReason is { } reason)
            CloseAlarms(openAlarms, reason, now);

        return Result.Success();
    }

    public Result Delete(IReadOnlyList<SignalOccurrence> openAlarms, DateTimeOffset now)
    {
        if (IsBuiltIn)
            return SignalErrors.BuiltInCannotBeDeleted();

        CloseAlarms(openAlarms, ClearReason.Deleted, now);
        Raise(new SignalDeleted(Id, OwnerId, now));

        return Result.Success();
    }

    public SignalCheckOutcome Check(IReadOnlyList<SignalOccurrence> openAlarms, SubjectReadings readings, DateTimeOffset now)
    {
        var outcome = SignalCheck.Apply(this, openAlarms, readings, now);

        LastCheckedAt = now;

        RaiseFired(outcome, now);
        RaiseCleared(ClearReason.Recovered, outcome.Recovered, outcome.StillActive, now);
        RaiseCleared(ClearReason.PositionClosed, outcome.Gone, outcome.StillActive, now);

        return outcome;
    }

    public bool IsDue(DateTimeOffset now, Func<DateTimeOffset, DateTime> toLocal)
    {
        if (!Enabled)
            return false;

        if (LastCheckedAt is not { } lastChecked)
            return true;

        if (Schedule.Last(toLocal(now)) is { } scheduledAt)
            return scheduledAt > toLocal(lastChecked);

        return now - lastChecked >= DataPollInterval;
    }

    private static Error? ValidationError(string name, SignalTarget target, SignalCondition condition)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength)
            return SignalErrors.InvalidName();

        return target.CanBeMeasuredBy(condition.Metric) ? null : SignalErrors.MetricDoesNotFitTarget();
    }

    private ClearReason? ReasonToCloseAlarms(SignalSettings settings)
    {
        if (Enabled && !settings.Enabled)
            return ClearReason.Disabled;

        return settings.Condition != Condition ? ClearReason.Reconfigured : null;
    }

    private void CloseAlarms(IReadOnlyList<SignalOccurrence> openAlarms, ClearReason reason, DateTimeOffset now)
    {
        foreach (var alarm in openAlarms)
            alarm.Clear(reason, now);

        RaiseCleared(reason, openAlarms, stillActive: 0, now);
    }

    private void RaiseFired(SignalCheckOutcome outcome, DateTimeOffset now)
    {
        if (outcome.TriggerId is not { } triggerId)
            return;

        var subjects = outcome.Fired.Select(alarm => alarm.Reading).ToList();

        Raise(new SignalFired(triggerId, Id, OwnerId, Target.AccountId, Name, Condition, subjects, now));
    }

    private void RaiseCleared(ClearReason reason, IReadOnlyList<SignalOccurrence> cleared, int stillActive, DateTimeOffset now)
    {
        if (cleared.Count == 0)
            return;

        var subjects = cleared.Select(alarm => alarm.SubjectLabel).ToList();

        Raise(new SignalCleared(Id, OwnerId, Name, reason, subjects, stillActive, now));
    }
}
