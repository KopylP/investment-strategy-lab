using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.Integration;

internal sealed class SignalClearedMapper : IDomainEventMapper<SignalCleared, SignalClearedV1>
{
    public SignalClearedV1 Map(SignalCleared cleared) => new(
        SignalId: cleared.SignalId,
        OwnerId: cleared.OwnerId.Value,
        Name: cleared.SignalName,
        Reason: cleared.Reason.ToString(),
        Subjects: cleared.Subjects,
        StillActive: cleared.StillActive,
        ClearedAt: cleared.OccurredAt);
}
