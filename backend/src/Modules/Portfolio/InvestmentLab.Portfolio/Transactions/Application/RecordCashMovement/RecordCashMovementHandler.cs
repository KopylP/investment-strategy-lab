using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;

/// <param name="Kind">Contribution, Withdrawal, Fee, TaxPayment or Dividend.</param>
/// <param name="Allocations">Null: split by target shares, or by held units for a dividend of an instrument.</param>
public sealed record RecordCashMovementCommand(
    string Kind,
    decimal Amount,
    DateOnly OccurredOn,
    IReadOnlyList<AmountAllocationDto>? Allocations = null,
    Guid? InstrumentId = null);

public sealed record RecordContributionCommand(decimal Amount, DateOnly OccurredOn, IReadOnlyList<AmountAllocationDto>? Allocations = null);

public sealed class RecordCashMovementHandler(TransactionRecorder recorder)
{
    public Task<Result<RecordedTransaction>> Handle(Guid accountId, RecordCashMovementCommand command, TransactionSource source, CancellationToken cancellationToken) =>
        recorder.RecordAsync(accountId, context => Create(context, command, source), cancellationToken);

    public Task<Result<RecordedTransaction>> Handle(Guid accountId, RecordContributionCommand command, TransactionSource source, CancellationToken cancellationToken) =>
        Handle(accountId, new RecordCashMovementCommand(nameof(CashMovementKind.Contribution), command.Amount, command.OccurredOn, command.Allocations), source, cancellationToken);

    private static Result<Transaction> Create(RecordingContext context, RecordCashMovementCommand command, TransactionSource source)
    {
        var currency = context.Account.BaseCurrency;
        var kind = TransactionInput.ParseEnum<CashMovementKind>(command.Kind, "transaction.cash_kind_unknown");
        if (kind.IsFailure)
            return kind.Error;
        if (command.Amount <= 0m)
            return TransactionErrors.AmountNotPositive();

        var amount = Money.Of(command.Amount, currency);
        var allocations = command.Allocations is { Count: > 0 } given
            ? TransactionInput.Amounts(given, currency)
            : Result<IReadOnlyList<CashAllocation>>.Success(TradeAllocator.Split(amount,
                kind.Value == CashMovementKind.Dividend && command.InstrumentId is { } instrumentId
                    ? TransactionInput.HeldWeights(context.Holdings, instrumentId)
                    : TransactionInput.TargetWeights(context.Account)));
        if (allocations.IsFailure)
            return allocations.Error;

        return Transaction.CashMovement(context.Account, kind.Value, amount, allocations.Value, command.InstrumentId, command.OccurredOn, source, context.Now);
    }
}
