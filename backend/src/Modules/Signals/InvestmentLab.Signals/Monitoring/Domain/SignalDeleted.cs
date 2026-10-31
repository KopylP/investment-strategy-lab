namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SignalDeleted(Guid SignalId, UserId OwnerId, DateTimeOffset OccurredAt) : IDomainEvent;
