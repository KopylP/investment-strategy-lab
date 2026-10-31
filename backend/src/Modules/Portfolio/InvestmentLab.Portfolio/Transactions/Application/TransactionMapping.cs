using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application;

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
