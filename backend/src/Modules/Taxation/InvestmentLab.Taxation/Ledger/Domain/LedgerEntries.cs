namespace InvestmentLab.Taxation.Ledger.Domain;

public static class LedgerEntries
{
    private const string Buy = "Buy";
    private const string Dividend = "Dividend";
    private const int CurrencyCodeLength = 3;
    private const int PriceDigits = 8;
    private const int FeeDigits = 2;

    public static TaxLedgerEntry? From(ITaxJournalEntry recorded, DateTimeOffset recordedAt)
    {
        if (recorded.Trade is { } trade)
            return TradeEntry(recorded, trade, recordedAt);

        if (recorded.Cash is { Kind: Dividend, InstrumentId: { } instrumentId } cash)
            return DividendEntry(recorded, cash, instrumentId, recordedAt);

        return null;
    }

    private static TaxLedgerEntry TradeEntry(ITaxJournalEntry recorded, ITaxJournalTrade trade, DateTimeOffset recordedAt)
    {
        var (price, fee, currency) = SettledTerms(recorded, trade);

        return TaxLedgerEntry.Trade(
            transactionId: recorded.TransactionId,
            accountId: recorded.AccountId,
            ownerId: recorded.OwnerId,
            occurredOn: recorded.OccurredOn,
            recordedAt: recordedAt,
            isBuy: trade.Side == Buy,
            instrumentId: trade.InstrumentId,
            quantity: trade.Quantity,
            price: price,
            fee: fee,
            currency: currency,
            allocations: trade.Allocations);
    }

    private static (decimal Price, decimal Fee, string Currency) SettledTerms(ITaxJournalEntry recorded, ITaxJournalTrade trade)
    {
        var total = trade.Side == Buy ? trade.Quantity * trade.Price + trade.Fee : trade.Quantity * trade.Price - trade.Fee;

        if (trade is { SettledAmount: > 0m and var settled, SettledCurrency: { Length: CurrencyCodeLength } own } && own != recorded.Currency && total > 0m)
        {
            var back = settled / total;

            return (decimal.Round(trade.Price * back, PriceDigits), decimal.Round(trade.Fee * back, FeeDigits), own);
        }

        return (trade.Price, trade.Fee, recorded.Currency);
    }

    private static TaxLedgerEntry DividendEntry(ITaxJournalEntry recorded, ITaxJournalCash cash, Guid instrumentId, DateTimeOffset recordedAt)
    {
        var (amount, currency) = cash is { SettledAmount: > 0m and var settled, SettledCurrency: { Length: CurrencyCodeLength } own } && own != recorded.Currency
            ? (settled, own)
            : (cash.Amount, recorded.Currency);

        return TaxLedgerEntry.Dividend(
            transactionId: recorded.TransactionId,
            accountId: recorded.AccountId,
            ownerId: recorded.OwnerId,
            occurredOn: recorded.OccurredOn,
            recordedAt: recordedAt,
            instrumentId: instrumentId,
            amount: amount,
            currency: currency,
            allocations: cash.Allocations);
    }
}
