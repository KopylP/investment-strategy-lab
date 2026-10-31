using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public static class LedgerEntries
{
    public static TaxLedgerEntry? From(TransactionRecordedV1 recorded, DateTimeOffset recordedAt)
    {
        var owner = UserId.Of(recorded.OwnerId);

        if (recorded.Trade is { } trade)
        {
            var (price, fee, currency) = (trade.Price, trade.Fee, recorded.Currency);
            var total = trade.Side == "Buy" ? trade.Quantity * trade.Price + trade.Fee : trade.Quantity * trade.Price - trade.Fee;
            if (trade is { SettledAmount: > 0m and var settled, SettledCurrency: { Length: 3 } own } && own != recorded.Currency && total > 0m)
            {
                var back = settled / total;
                (price, fee, currency) = (decimal.Round(trade.Price * back, 8), decimal.Round(trade.Fee * back, 2), own);
            }

            return TaxLedgerEntry.Trade(recorded.TransactionId, recorded.AccountId, owner, recorded.OccurredOn, recordedAt,
                trade.Side == "Buy", trade.InstrumentId, trade.Quantity, price, fee, currency,
                [.. trade.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Quantity))]);
        }

        if (recorded.Cash is { Kind: "Dividend", InstrumentId: { } instrumentId } cash)
        {
            var (amount, currency) = cash is { SettledAmount: > 0m and var settled, SettledCurrency: { Length: 3 } own } && own != recorded.Currency
                ? (settled, own)
                : (cash.Amount, recorded.Currency);
            return TaxLedgerEntry.Dividend(recorded.TransactionId, recorded.AccountId, owner, recorded.OccurredOn, recordedAt,
                instrumentId, amount, currency, [.. cash.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Amount))]);
        }

        return null;
    }
}
