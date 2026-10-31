namespace InvestmentLab.Signals.Contracts;

public sealed record SignalDeletedV1(Guid SignalId, Guid OwnerId, DateTimeOffset DeletedAt);
