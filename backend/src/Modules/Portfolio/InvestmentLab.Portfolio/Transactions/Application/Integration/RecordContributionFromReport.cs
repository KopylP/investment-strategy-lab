using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordContribution;
using InvestmentLab.Portfolio.Transactions.Application.Shared;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class RecordContributionFromReport(
    RecordContributionHandler contributions,
    CommandRejection rejection) : IIntegrationEventHandler<RecordContributionV1>
{
    public async Task Handle(RecordContributionV1 command, CancellationToken cancellationToken)
    {
        var contribution = new RecordContributionCommand(command.Amount, command.OccurredOn, command.Allocations);
        var source = TransactionSourceDto.FromReport(command.SourceReference);

        var recorded = await contributions.Handle(command.AccountId, contribution, source, cancellationToken);

        await rejection.RejectOnFailureAsync(command.SourceReference, recorded, cancellationToken);
    }
}
