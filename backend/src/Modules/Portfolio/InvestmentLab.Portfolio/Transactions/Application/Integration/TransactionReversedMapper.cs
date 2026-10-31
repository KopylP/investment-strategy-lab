using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class TransactionReversedMapper : IDomainEventMapper<TransactionReversed, TransactionReversedV1>
{
    public TransactionReversedV1 Map(TransactionReversed reversed) =>
        reversed.Reversal.ToReversedEvent(reversed.Original);
}
