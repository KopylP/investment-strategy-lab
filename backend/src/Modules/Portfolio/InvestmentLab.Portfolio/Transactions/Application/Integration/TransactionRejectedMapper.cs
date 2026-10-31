using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class TransactionRejectedMapper : IDomainEventMapper<TransactionRejected, TransactionRejectedV1>
{
    public TransactionRejectedV1 Map(TransactionRejected rejected) =>
        new(rejected.SourceReference, rejected.Code, rejected.Reason);
}
