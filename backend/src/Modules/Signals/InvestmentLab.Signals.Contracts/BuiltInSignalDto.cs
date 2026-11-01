namespace InvestmentLab.Signals.Contracts;

public sealed record BuiltInSignalDto(Guid SignalId, Guid OwnerId, Guid? AccountId, string Kind);
