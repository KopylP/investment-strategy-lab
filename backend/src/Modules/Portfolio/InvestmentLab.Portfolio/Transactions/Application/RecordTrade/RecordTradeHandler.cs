using InvestmentLab.Portfolio.Accounts.Domain;
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
    public async Task<Result<RecordedTransaction>> Handle(
        Guid accountId, RecordTradeCommand command, TransactionSourceDto source, CancellationToken cancellationToken)
    {
        var origin = source.ToDomain();
        if (origin.IsFailure)
            return origin.Error;

        return await recorder.RecordAsync(accountId, context => Create(context, command, origin.Value), cancellationToken);
    }

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

        if (side == TradeSide.Buy)
            return IntoTheOnlySubPortfolio(context.Account, quantity);

        var holdingWeights = context.Holdings.WeightsOf(command.InstrumentId);

        return Result<IReadOnlyList<Allocation>>.Success(TradeAllocator.Split(quantity, holdingWeights));
    }

    private static Result<IReadOnlyList<Allocation>> IntoTheOnlySubPortfolio(InvestmentAccount account, Quantity quantity)
    {
        var active = account.ActiveSubPortfolios.ToList();
        if (active.Count != 1)
            return TransactionErrors.SubPortfolioRequired(active.Count);

        return Result<IReadOnlyList<Allocation>>.Success([new Allocation(active[0].Id, quantity)]);
    }
}
