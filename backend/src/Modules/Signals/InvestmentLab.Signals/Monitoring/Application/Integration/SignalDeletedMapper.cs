using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.Integration;

internal sealed class SignalDeletedMapper : IDomainEventMapper<SignalDeleted, SignalDeletedV1>
{
    public SignalDeletedV1 Map(SignalDeleted deleted) =>
        new(deleted.SignalId, deleted.OwnerId.Value, deleted.OccurredAt);
}
