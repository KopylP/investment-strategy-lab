using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class TransactionRecordedMapper : IDomainEventMapper<TransactionRecorded, TransactionRecordedV1>
{
    public TransactionRecordedV1 Map(TransactionRecorded recorded) =>
        recorded.Transaction.ToRecordedEvent(recorded.Currency);
}
