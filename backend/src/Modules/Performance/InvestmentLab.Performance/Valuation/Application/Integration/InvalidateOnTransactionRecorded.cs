using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class InvalidateOnTransactionRecorded(SnapshotInvalidation snapshots) : IIntegrationEventHandler<TransactionRecordedV1>
{
    public Task Handle(TransactionRecordedV1 recorded, CancellationToken cancellationToken) =>
        snapshots.MarkAccountAsync(recorded.AccountId, recorded.OwnerId, recorded.OccurredOn, cancellationToken);
}
