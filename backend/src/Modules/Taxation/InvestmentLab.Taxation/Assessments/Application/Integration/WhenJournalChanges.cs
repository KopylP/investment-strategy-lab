using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Taxation.Assessments.Application.Integration;

/// <summary>
/// Keeps Taxation's copy of the journal (§9.6) and the account's tax current after every change; redelivery is harmless.
/// </summary>
internal sealed class WhenJournalChanges(TaxRecalculator recalculator)
    : IIntegrationEventHandler<TransactionRecordedV1>, IIntegrationEventHandler<TransactionReversedV1>
{
    // The recalculator catches the ledger copy up from the journal first, so both events just trigger it.
    public async Task Handle(TransactionRecordedV1 recorded, CancellationToken cancellationToken)
    {
        if (recorded.Trade is not null || recorded.Cash is { Kind: "Dividend" })
            await recalculator.RecomputeAsync(recorded.AccountId, cancellationToken);
    }

    public Task Handle(TransactionReversedV1 reversed, CancellationToken cancellationToken) =>
        recalculator.RecomputeAsync(reversed.AccountId, cancellationToken);
}
