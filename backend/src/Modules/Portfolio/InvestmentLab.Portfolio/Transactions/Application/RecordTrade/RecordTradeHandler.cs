using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.RecordTrade;

public sealed record RecordTradeCommand(
    Guid InstrumentId,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    DateOnly ExecutedOn,
    IReadOnlyList<QuantityAllocationDto>? Allocations = null);

public sealed class RecordTradeHandler(TransactionRecorder recorder)
{
    public Task<Result<RecordedTransaction>> Handle(
        Guid accountId,
        RecordTradeCommand command,
        TransactionSource source,
        CancellationToken cancellationToken) =>
        recorder.RecordAsync(accountId, context => Create(context, command, source), cancellationToken);

    private static Result<Transaction> Create(RecordingContext context, RecordTradeCommand command, TransactionSource source)
    {
        var currency = context.Account.BaseCurrency;
        var side = EnumText.TryParse<TradeSide>(command.Side, "transaction.side_unknown");
        if (side.IsFailure)
            return side.Error;

        var quantity = Quantity.TryOf(command.Quantity);
        if (quantity.IsFailure)
            return quantity.Error;

        var price = Price.TryOf(command.Price, currency);
        if (price.IsFailure)
            return price.Error;

        var allocations = AllocationsOf(context, command, side.Value, quantity.Value);
        if (allocations.IsFailure)
            return allocations.Error;

        if (allocations.Value.Count == 0 && !quantity.Value.IsNone)
            return TransactionErrors.NothingToAllocateTo(command.InstrumentId);

        return Transaction.Trade(context.Account, side.Value, command.InstrumentId, quantity.Value, price.Value, Money.Of(command.Fee, currency),
            allocations.Value, command.ExecutedOn, source, context.Now);
    }

    private static Result<IReadOnlyList<Allocation>> AllocationsOf(RecordingContext context, RecordTradeCommand command, TradeSide side, Quantity quantity)
    {
        if (command.Allocations is { Count: > 0 } given)
            return Allocation.TryAll(given.Select(allocation => (allocation.SubPortfolioId, allocation.Quantity)));

        var weights = side == TradeSide.Buy
            ? context.Account.TargetWeights()
            : context.Holdings.WeightsOf(command.InstrumentId);

        return Result<IReadOnlyList<Allocation>>.Success(TradeAllocator.Split(quantity, weights));
    }
}
