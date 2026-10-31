using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Application;

public static class MovementMapper
{
    public static IReadOnlyList<Movement> From(JournalSnapshotDto journal)
    {
        var reversed = journal.ReversedIds.ToHashSet();
        var movements = new List<Movement>();
        var sequence = 0;

        foreach (var entry in journal.Entries.Where(entry => !reversed.Contains(entry.TransactionId)))
        {
            var day = entry.OccurredOn;

            if (entry.Trade is { } trade)
            {
                var sign = trade.Side == "Buy" ? 1m : -1m;
                var total = trade.Allocations.Sum(allocation => allocation.Quantity);

                foreach (var allocation in trade.Allocations)
                {
                    var share = total > 0m ? allocation.Quantity / total : 0m;

                    var (cashDelta, currency) = trade is { SettledAmount: { } settled, SettledCurrency: { } code }
                        ? (-sign * settled * share, code)
                        : (-sign * allocation.Quantity * trade.Price - trade.Fee * share, (string?)null);
                    movements.Add(new Movement(day, sequence++, allocation.SubPortfolioId, trade.InstrumentId, sign * allocation.Quantity,
                        cashDelta, 0m, TradePrice: trade.Price, CashCurrency: currency,
                        UnitCost: allocation.Quantity > 0m ? trade.Price + trade.Fee * share / allocation.Quantity : null));
                }
            }
            else if (entry.Cash is { } cash)
            {
                var total = cash.Allocations.Sum(allocation => allocation.Amount);

                foreach (var allocation in cash.Allocations)
                {
                    var (cashDelta, flow) = cash.Kind switch
                    {
                        "Contribution" => (allocation.Amount, allocation.Amount),
                        "Withdrawal" => (-allocation.Amount, -allocation.Amount),
                        "Dividend" => (allocation.Amount, 0m),
                        _ => (-allocation.Amount, 0m),
                    };

                    if (cash is { SettledAmount: { } settled, SettledCurrency: { } code } && total > 0m)
                        movements.Add(new Movement(day, sequence++, allocation.SubPortfolioId, null, 0m,
                            Math.Sign(cashDelta) * settled * allocation.Amount / total, flow, CashCurrency: code));
                    else
                        movements.Add(new Movement(day, sequence++, allocation.SubPortfolioId, null, 0m, cashDelta, flow));
                }
            }
            else if (entry.Transfer is { } transfer)
            {
                if (transfer is { InstrumentId: { } instrument, Quantity: { } quantity })
                {
                    movements.Add(new Movement(day, sequence++, transfer.FromSubPortfolioId, instrument, -quantity, 0m, 0m, TransferQuantity: -quantity));
                    movements.Add(new Movement(day, sequence++, transfer.ToSubPortfolioId, instrument, quantity, 0m, 0m, TransferQuantity: quantity));
                }
                else if (transfer.Amount is { } amount)
                {
                    movements.Add(new Movement(day, sequence++, transfer.FromSubPortfolioId, null, 0m, -amount, -amount));
                    movements.Add(new Movement(day, sequence++, transfer.ToSubPortfolioId, null, 0m, amount, amount));
                }
            }
        }

        return movements;
    }
}
