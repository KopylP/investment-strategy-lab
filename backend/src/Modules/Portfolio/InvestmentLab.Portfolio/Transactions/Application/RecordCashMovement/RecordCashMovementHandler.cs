using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.RecordCashMovement;

public sealed record RecordCashMovementCommand(
    string Kind,
    decimal Amount,
    DateOnly OccurredOn,
    IReadOnlyList<AmountAllocationDto>? Allocations = null,
    Guid? InstrumentId = null);

public sealed class RecordCashMovementHandler(TransactionRecorder recorder) : IHandler
{
    public async Task<Result<RecordedTransaction>> Handle(
        Guid accountId, RecordCashMovementCommand command, TransactionSourceDto source, CancellationToken cancellationToken)
    {
        var origin = source.ToDomain();
        if (origin.IsFailure)
            return origin.Error;

        return await recorder.RecordAsync(accountId, context => Create(context, command, origin.Value), cancellationToken);
    }

    private static Result<Transaction> Create(RecordingContext context, RecordCashMovementCommand command, TransactionSource source)
    {
        var kind = EnumText.TryParse<CashMovementKind>(command.Kind, "transaction.cash_kind_unknown");
        if (kind.IsFailure)
            return kind.Error;

        if (command.Amount <= 0m)
            return TransactionErrors.AmountNotPositive();

        var amount = Money.Of(command.Amount, context.Account.BaseCurrency);

        var allocations = AllocationsOf(context, command, kind.Value, amount);
        if (allocations.IsFailure)
            return allocations.Error;

        return Transaction.CashMovement(
            context.Account, kind.Value, amount, allocations.Value, command.InstrumentId, command.OccurredOn, source, context.Now);
    }

    private static Result<IReadOnlyList<CashAllocation>> AllocationsOf(
        RecordingContext context, RecordCashMovementCommand command, CashMovementKind kind, Money amount)
    {
        if (command.Allocations is { Count: > 0 } given)
            return CashAllocation.TryAll(given.Select(allocation => (allocation.SubPortfolioId, allocation.Amount)), context.Account.BaseCurrency);

        var weights = kind == CashMovementKind.Dividend && command.InstrumentId is { } instrumentId
            ? context.Holdings.WeightsOf(instrumentId)
            : context.Account.TargetWeights();

        return Result<IReadOnlyList<CashAllocation>>.Success(TradeAllocator.Split(amount, weights));
    }
}
