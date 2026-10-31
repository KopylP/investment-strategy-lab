using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Taxation.Assessments.Application.Integration;

internal sealed class RecomputeOnTransactionRecorded(TaxRecalculator recalculator) : IIntegrationEventHandler<TransactionRecordedV1>
{
    public async Task Handle(TransactionRecordedV1 recorded, CancellationToken cancellationToken)
    {
        if (IsTaxable(recorded))
            await recalculator.RecomputeAsync(recorded.AccountId, cancellationToken);
    }

    private static bool IsTaxable(TransactionRecordedV1 recorded) =>
        recorded.Trade is not null || recorded.Cash is { Kind: "Dividend" };
}
