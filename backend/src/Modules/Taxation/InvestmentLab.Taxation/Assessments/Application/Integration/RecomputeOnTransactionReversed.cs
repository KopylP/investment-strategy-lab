using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Assessments.Application.Shared;

namespace InvestmentLab.Taxation.Assessments.Application.Integration;

internal sealed class RecomputeOnTransactionReversed(TaxRecalculator recalculator) : IIntegrationEventHandler<TransactionReversedV1>
{
    public Task Handle(TransactionReversedV1 reversed, CancellationToken cancellationToken) =>
        recalculator.RecomputeAsync(reversed.AccountId, cancellationToken);
}
