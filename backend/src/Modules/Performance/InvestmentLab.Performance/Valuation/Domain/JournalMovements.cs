namespace InvestmentLab.Performance.Valuation.Domain;

public static class JournalMovements
{
    private const string Buy = "Buy";
    private const string Contribution = "Contribution";
    private const string Withdrawal = "Withdrawal";
    private const string Dividend = "Dividend";

    public static IReadOnlyList<Movement> Of(AccountJournal journal)
    {
        var reversed = journal.ReversedIds.ToHashSet();
        var movements = new List<Movement>();

        foreach (var entry in journal.Entries.Where(entry => !reversed.Contains(entry.TransactionId)))
            AddMovementsOf(entry, movements);

        return movements;
    }

    private static void AddMovementsOf(IJournalEntry entry, List<Movement> movements)
    {
        if (entry.Trade is { } trade)
            AddTrade(entry.OccurredOn, trade, movements);
        else if (entry.Cash is { } cash)
            AddCashMovement(entry.OccurredOn, cash, movements);
        else if (entry.Transfer is { } transfer)
            AddTransfer(entry.OccurredOn, transfer, movements);
    }

    private static void AddTrade(DateOnly day, IJournalTrade trade, List<Movement> movements)
    {
        var sign = trade.Side == Buy ? 1m : -1m;
        var total = trade.Allocations.Sum(allocation => allocation.Quantity);

        foreach (var allocation in trade.Allocations)
        {
            var share = total > 0m ? allocation.Quantity / total : 0m;
            var (cashDelta, currency) = TradeCash(trade, allocation, sign, share);
            var unitCost = allocation.Quantity > 0m ? trade.Price + trade.Fee * share / allocation.Quantity : (decimal?)null;

            movements.Add(new Movement(
                day,
                movements.Count,
                allocation.SubPortfolioId,
                trade.InstrumentId,
                QuantityDelta: sign * allocation.Quantity,
                CashDelta: cashDelta,
                ExternalFlow: 0m,
                TradePrice: trade.Price,
                CashCurrency: currency,
                UnitCost: unitCost));
        }
    }

    private static (decimal CashDelta, string? Currency) TradeCash(
        IJournalTrade trade,
        IJournalQuantity allocation,
        decimal sign,
        decimal share)
    {
        if (trade is { SettledAmount: { } settled, SettledCurrency: { } code })
            return (-sign * settled * share, code);

        return (-sign * allocation.Quantity * trade.Price - trade.Fee * share, null);
    }

    private static void AddCashMovement(DateOnly day, IJournalCash cash, List<Movement> movements)
    {
        var total = cash.Allocations.Sum(allocation => allocation.Amount);

        foreach (var allocation in cash.Allocations)
        {
            var (cashDelta, flow) = CashAndFlow(cash.Kind, allocation.Amount);

            if (cash is { SettledAmount: { } settled, SettledCurrency: { } code } && total > 0m)
            {
                var settledDelta = Math.Sign(cashDelta) * settled * allocation.Amount / total;
                movements.Add(CashOnly(day, movements.Count, allocation.SubPortfolioId, settledDelta, flow, code));
            }
            else
                movements.Add(CashOnly(day, movements.Count, allocation.SubPortfolioId, cashDelta, flow));
        }
    }

    private static (decimal CashDelta, decimal Flow) CashAndFlow(string kind, decimal amount) => kind switch
    {
        Contribution => (amount, amount),
        Withdrawal => (-amount, -amount),
        Dividend => (amount, 0m),
        _ => (-amount, 0m),
    };

    private static void AddTransfer(DateOnly day, IJournalTransfer transfer, List<Movement> movements)
    {
        var from = transfer.FromSubPortfolioId;
        var to = transfer.ToSubPortfolioId;

        if (transfer is { InstrumentId: { } instrument, Quantity: { } quantity })
        {
            movements.Add(PositionTransfer(day, movements.Count, from, instrument, -quantity));
            movements.Add(PositionTransfer(day, movements.Count, to, instrument, quantity));
        }
        else if (transfer.Amount is { } amount)
        {
            movements.Add(CashOnly(day, movements.Count, from, -amount, -amount));
            movements.Add(CashOnly(day, movements.Count, to, amount, amount));
        }
    }

    private static Movement PositionTransfer(DateOnly day, int sequence, Guid subPortfolioId, Guid instrumentId, decimal quantity) =>
        new(day, sequence, subPortfolioId, instrumentId, quantity, CashDelta: 0m, ExternalFlow: 0m, TransferQuantity: quantity);

    private static Movement CashOnly(DateOnly day, int sequence, Guid subPortfolioId, decimal cashDelta, decimal flow, string? currency = null) =>
        new(day, sequence, subPortfolioId, InstrumentId: null, QuantityDelta: 0m, cashDelta, flow, CashCurrency: currency);
}
