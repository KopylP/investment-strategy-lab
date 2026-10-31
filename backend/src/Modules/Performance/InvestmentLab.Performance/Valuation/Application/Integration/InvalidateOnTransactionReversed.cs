using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class InvalidateOnTransactionReversed(SnapshotInvalidation snapshots) : IIntegrationEventHandler<TransactionReversedV1>
{
    public Task Handle(TransactionReversedV1 reversed, CancellationToken cancellationToken) =>
        snapshots.MarkAccountAsync(reversed.AccountId, reversed.OwnerId, reversed.OriginalOccurredOn, cancellationToken);
}
