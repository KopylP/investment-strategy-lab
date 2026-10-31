using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.RecordContribution;

public sealed record RecordContributionCommand(decimal Amount, DateOnly OccurredOn, IReadOnlyList<AmountAllocationDto>? Allocations = null);

public sealed class RecordContributionHandler(RecordCashMovementHandler cashMovements)
{
    public Task<Result<RecordedTransaction>> Handle(
        Guid accountId, RecordContributionCommand command, TransactionSource source, CancellationToken cancellationToken)
    {
        var movement = new RecordCashMovementCommand(nameof(CashMovementKind.Contribution), command.Amount, command.OccurredOn, command.Allocations);

        return cashMovements.Handle(accountId, movement, source, cancellationToken);
    }
}
