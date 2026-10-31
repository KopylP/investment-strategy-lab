using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ConfirmStatementImport;

public static class StatementRowTransaction
{
    public static Result<Transaction> Of(RecordingContext context, PricedRow row, string reference)
    {
        var source = new TransactionSource(TransactionSourceKind.Import, reference);
        var sub = SubPortfolioId.Of(row.SubPortfolioId!.Value);

        return row.Row.Kind switch
        {
            StatementRowKind.Sell when IsSpinOffSale(context, row, sub) => SpinOffIncome(context, row, sub, source),
            StatementRowKind.Buy or StatementRowKind.Sell => Trade(context, row, sub, source),
            _ => CashMovement(context, row, sub, source),
        };
    }

    private static bool IsSpinOffSale(RecordingContext context, PricedRow row, SubPortfolioId sub) =>
        row.InstrumentId is { } instrument
        && context.Holdings.UnitsOf(sub, instrument).Value < row.Quantity
        && !context.Journal.Any(entry => entry.Details is TradeDetails { Side: TradeSide.Buy } trade && trade.InstrumentId == instrument);

    private static Result<Transaction> SpinOffIncome(RecordingContext context, PricedRow row, SubPortfolioId sub, TransactionSource source)
    {
        var proceeds = Money.Of(decimal.Round(row.Amount, 2), context.Account.BaseCurrency);
        CashAllocation[] allocations = [new CashAllocation(sub, proceeds)];

        return Transaction.CashMovement(
            context.Account,
            CashMovementKind.Dividend,
            proceeds,
            allocations,
            row.InstrumentId,
            row.Row.OccurredOn,
            source,
            context.Now,
            row.Settlement);
    }

    private static Result<Transaction> Trade(RecordingContext context, PricedRow row, SubPortfolioId sub, TransactionSource source)
    {
        var currency = context.Account.BaseCurrency;

        var quantity = Quantity.TryOf(row.Quantity);
        var price = Price.TryOf(decimal.Round(row.Price, 6), currency);
        if (quantity.IsFailure || price.IsFailure)
            return quantity.IsFailure ? quantity.Error : price.Error;

        Allocation[] allocations = [new Allocation(sub, quantity.Value)];

        return Transaction.Trade(
            context.Account,
            row.Row.Kind == StatementRowKind.Buy ? TradeSide.Buy : TradeSide.Sell,
            row.InstrumentId!.Value,
            quantity.Value,
            price.Value,
            Money.Of(decimal.Round(row.Fee, 2), currency),
            allocations,
            row.Row.OccurredOn,
            source,
            context.Now,
            row.Settlement);
    }

    private static Result<Transaction> CashMovement(RecordingContext context, PricedRow row, SubPortfolioId sub, TransactionSource source)
    {
        var kind = row.Row.Kind switch
        {
            StatementRowKind.Contribution => CashMovementKind.Contribution,
            StatementRowKind.Withdrawal => CashMovementKind.Withdrawal,
            _ => CashMovementKind.Dividend,
        };
        var amount = Money.Of(row.Amount, context.Account.BaseCurrency);
        CashAllocation[] allocations = [new CashAllocation(sub, amount)];

        return Transaction.CashMovement(
            context.Account,
            kind,
            amount,
            allocations,
            kind == CashMovementKind.Dividend ? row.InstrumentId : null,
            row.Row.OccurredOn,
            source,
            context.Now,
            row.Settlement);
    }
}
