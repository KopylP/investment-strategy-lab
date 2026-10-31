namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SignalFired(
    Guid TriggerId,
    Guid SignalId,
    UserId OwnerId,
    Guid? AccountId,
    string SignalName,
    SignalCondition Condition,
    IReadOnlyList<SubjectReading> Subjects,
    DateTimeOffset OccurredAt) : IDomainEvent;
