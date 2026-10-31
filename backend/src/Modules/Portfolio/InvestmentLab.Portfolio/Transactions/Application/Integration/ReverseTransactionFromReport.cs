using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.ReverseTransaction;

namespace InvestmentLab.Portfolio.Transactions.Application.Integration;

internal sealed class ReverseTransactionFromReport(ReverseTransactionHandler reversals, CommandRejection rejection) : IIntegrationEventHandler<ReverseTransactionV1>
{
    public async Task Handle(ReverseTransactionV1 command, CancellationToken cancellationToken)
    {
        var reversed = await reversals.HandleBySource(command.AccountId, command.SourceReference, command.Reason, cancellationToken);

        await rejection.RejectOnFailureAsync(command.SourceReference, reversed, cancellationToken);
    }
}
