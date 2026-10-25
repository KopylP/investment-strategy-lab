using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application;

internal static class TransactionInput
{
    public static Result<TEnum> ParseEnum<TEnum>(string? text, string code) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) && Enum.IsDefined(value) && !int.TryParse(text, out _)
            ? value
            : Error.Validation(code, $"'{text}' is not one of {string.Join(", ", Enum.GetNames<TEnum>())}.");

    /// <summary>Weights for new money and buys: target shares of the active sub-portfolios.</summary>
    public static IReadOnlyList<(SubPortfolioId, decimal)> TargetWeights(InvestmentAccount account) =>
        [.. account.ActiveSubPortfolios.Select(sub => (sub.Id, sub.TargetShare.Value.Value))];

    /// <summary>Weights for sales and dividends: units of the instrument each sub-portfolio holds.</summary>
    public static IReadOnlyList<(SubPortfolioId, decimal)> HeldWeights(AccountHoldings holdings, Guid instrumentId) =>
        [.. holdings.SubPortfolios.Select(sub => (sub.SubPortfolioId, sub.UnitsOf(instrumentId).Value))];

    public static Result<IReadOnlyList<Allocation>> Quantities(IReadOnlyList<QuantityAllocationDto> inputs)
    {
        var allocations = new List<Allocation>();
        foreach (var input in inputs)
        {
            var quantity = Quantity.TryOf(input.Quantity);
            if (quantity.IsFailure || quantity.Value.IsNone)
                return TransactionErrors.QuantityNotPositive();
            if (input.SubPortfolioId == Guid.Empty)
                return TransactionErrors.UnknownSubPortfolio(input.SubPortfolioId);
            allocations.Add(new Allocation(SubPortfolioId.Of(input.SubPortfolioId), quantity.Value));
        }

        return allocations;
    }

    public static Result<IReadOnlyList<CashAllocation>> Amounts(IReadOnlyList<AmountAllocationDto> inputs, Currency currency)
    {
        var allocations = new List<CashAllocation>();
        foreach (var input in inputs)
        {
            if (input.SubPortfolioId == Guid.Empty)
                return TransactionErrors.UnknownSubPortfolio(input.SubPortfolioId);
            allocations.Add(new CashAllocation(SubPortfolioId.Of(input.SubPortfolioId), Money.Of(input.Amount, currency)));
        }

        return allocations;
    }
}

internal static class TransactionMapping
{
    extension(Transaction transaction)
    {
        public TransactionRecordedV1 ToRecordedEvent(Currency currency) => new(
            transaction.Id.Value,
            transaction.AccountId.Value,
            transaction.OwnerId.Value,
            transaction.Kind.ToString(),
            transaction.OccurredOn,
            transaction.Source.Kind.ToString(),
            transaction.Source.Reference,
            currency.Code,
            transaction.Details is TradeDetails trade ? trade.ToDto() : null,
            transaction.Details is CashMovementDetails cash ? cash.ToDto() : null,
            transaction.Details is InternalTransferDetails transfer ? transfer.ToDto() : null);

        public TransactionReversedV1 ToReversedEvent(Transaction original) => new(
            original.Id.Value,
            transaction.Id.Value,
            transaction.AccountId.Value,
            transaction.OwnerId.Value,
            original.OccurredOn,
            original.Source.Reference,
            ((ReversalDetails)transaction.Details).Reason);
    }

    extension(TradeDetails trade)
    {
        public TradeRecordedDto ToDto() => new(
            trade.InstrumentId, trade.Side.ToString(), trade.Quantity.Value, trade.Price.Amount, trade.Fee.Amount,
            [.. trade.Allocations.Select(a => new QuantityAllocationDto(a.SubPortfolioId.Value, a.Quantity.Value))],
            trade.Settlement?.Amount, trade.Settlement?.Currency.Code);
    }

    extension(CashMovementDetails cash)
    {
        public CashMovementRecordedDto ToDto() => new(
            cash.Kind.ToString(), cash.Amount.Amount, cash.InstrumentId,
            [.. cash.Allocations.Select(a => new AmountAllocationDto(a.SubPortfolioId.Value, a.Amount.Amount))],
            cash.Settlement?.Amount, cash.Settlement?.Currency.Code);
    }

    extension(InternalTransferDetails transfer)
    {
        public InternalTransferRecordedDto ToDto() => new(
            transfer.From.Value, transfer.To.Value, transfer.InstrumentId, transfer.Quantity?.Value, transfer.Amount?.Amount);
    }
}
