using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordContribution;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class RecordContributionFromReport(RecordContributionHandler contributions, CommandRejection rejection) : IIntegrationEventHandler<RecordContributionV1>
{
    public async Task Handle(RecordContributionV1 command, CancellationToken cancellationToken)
    {
        var contribution = new RecordContributionCommand(command.Amount, command.OccurredOn, command.Allocations);
        var source = CommandRejection.FromReport with { Reference = command.SourceReference };

        var recorded = await contributions.Handle(command.AccountId, contribution, source, cancellationToken);

        await rejection.RejectOnFailureAsync(command.SourceReference, recorded, cancellationToken);
    }
}
