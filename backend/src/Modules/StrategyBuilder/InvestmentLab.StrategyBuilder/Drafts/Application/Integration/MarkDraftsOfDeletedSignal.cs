using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Integration;

internal sealed class MarkDraftsOfDeletedSignal(IDraftRepository drafts) : IIntegrationEventHandler<SignalDeletedV1>
{
    public async Task Handle(SignalDeletedV1 message, CancellationToken cancellationToken)
    {
        var referencing = await drafts.ReferencingSignalAsync(message.SignalId, cancellationToken);

        foreach (var draft in referencing.Where(draft => draft.OwnerId.Value == message.OwnerId))
            draft.MarkSignalMissing(message.SignalId);

        await drafts.SaveChangesAsync(cancellationToken);
    }
}
