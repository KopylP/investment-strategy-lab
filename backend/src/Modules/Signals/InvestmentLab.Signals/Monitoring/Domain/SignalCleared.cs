namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SignalCleared(
    Guid SignalId,
    UserId OwnerId,
    string SignalName,
    ClearReason Reason,
    IReadOnlyList<string> Subjects,
    int StillActive,
    DateTimeOffset OccurredAt) : IDomainEvent;
