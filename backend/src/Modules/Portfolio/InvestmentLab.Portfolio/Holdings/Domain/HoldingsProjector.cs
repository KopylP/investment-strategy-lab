using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Domain;

public static class HoldingsProjector
{
    public static Result<ProjectedHoldings> Project(InvestmentAccount account, IEnumerable<Transaction> journal, DateOnly? asOf = null)
    {
        var effective = EffectiveEntries([.. journal], asOf);
        var state = new Dictionary<SubPortfolioId, Sleeve>();
        Sleeve SleeveOf(SubPortfolioId id) => state.TryGetValue(id, out var sleeve) ? sleeve : state[id] = new Sleeve();

        foreach (var entry in effective)
        {
            if (Apply(entry, SleeveOf) is { } failure)
                return failure;
        }

        var subPortfolios = account.SubPortfolios
            .Where(sub => sub.IsActive || state.ContainsKey(sub.Id))
            .Select(sub => SleeveOf(sub.Id).Freeze(sub.Id, account.BaseCurrency))
            .ToList();

        var lastDate = effective.Count > 0 ? effective[^1].OccurredOn : (DateOnly?)null;
        var holdings = new AccountHoldings(account.Id, account.BaseCurrency, asOf ?? lastDate, subPortfolios);

        return new ProjectedHoldings(holdings, NegativeCashWarnings(account, state));
    }

    private static List<Transaction> EffectiveEntries(List<Transaction> entries, DateOnly? asOf)
    {
        var reversed = entries.Where(entry => entry.ReversalOf is not null).Select(entry => entry.ReversalOf!.Value).ToHashSet();

        return [.. entries
            .Where(entry => entry.Kind != TransactionKind.Reversal && !reversed.Contains(entry.Id))
            .Where(entry => asOf is null || entry.OccurredOn <= asOf)
            .OrderBy(entry => entry.OccurredOn).ThenBy(entry => entry.RecordedAt).ThenBy(entry => entry.Id.Value)];
    }

    private static Error? Apply(Transaction entry, Func<SubPortfolioId, Sleeve> sleeveOf) => entry.Details switch
    {
        TradeDetails trade => ApplyTrade(entry, trade, sleeveOf),
        CashMovementDetails cash => ApplyCash(cash, sleeveOf),
        InternalTransferDetails transfer => ApplyTransfer(entry, transfer, sleeveOf),
        _ => null,
    };

    private static List<string> NegativeCashWarnings(InvestmentAccount account, Dictionary<SubPortfolioId, Sleeve> state)
    {
        var overdrawn = account.SubPortfolios.Where(sub => state.TryGetValue(sub.Id, out var sleeve) && sleeve.Cash < 0m);

        return [.. overdrawn.Select(sub => NegativeCashWarning(sub.Name, state[sub.Id].Cash, account.BaseCurrency))];
    }

    private static string NegativeCashWarning(string name, decimal cash, Currency currency) =>
        $"Cash of '{name}' is {decimal.Round(cash, 2)} {currency}: record the contribution that paid for it.";

    private static Error? ApplyTrade(Transaction entry, TradeDetails trade, Func<SubPortfolioId, Sleeve> sleeveOf)
    {
        var fees = TradeAllocator.SplitFee(trade.Fee, trade.Allocations);
        var settlements = trade.Settlement is { } settlement
            ? Split(settlement.Amount, [.. trade.Allocations.Select(allocation => allocation.Quantity.Value)])
            : null;

        for (var i = 0; i < trade.Allocations.Count; i++)
        {
            var allocation = trade.Allocations[i];
            var sleeve = sleeveOf(allocation.SubPortfolioId);
            var fee = fees[i].Amount;
            var settled = settlements?[i];

            if (trade.Side == TradeSide.Buy)
                BuyInto(sleeve, entry, trade, allocation.Quantity, fee, settled);
            else if (SellFrom(sleeve, entry, trade, allocation, fee, settled) is { } failure)
                return failure;
        }

        return null;
    }

    private static void BuyInto(Sleeve sleeve, Transaction entry, TradeDetails trade, Quantity quantity, decimal fee, decimal? settled)
    {
        if (settled is { } paid)
            sleeve.AddForeign(trade.Settlement!.Currency, -paid);
        else
            sleeve.Cash -= trade.Price.TotalFor(quantity).Amount + fee;

        var unitCost = trade.Price.Amount + fee / quantity.Value;

        sleeve.LotsOf(trade.InstrumentId).Add(new Lot(entry.Id, entry.OccurredOn, quantity.Value, unitCost));
    }

    private static Error? SellFrom(Sleeve sleeve, Transaction entry, TradeDetails trade, Allocation allocation, decimal fee, decimal? settled)
    {
        var quantity = allocation.Quantity;

        if (sleeve.Take(trade.InstrumentId, quantity.Value) is null)
        {
            var held = sleeve.Units(trade.InstrumentId);

            return TransactionErrors.NotEnoughUnits(allocation.SubPortfolioId.Value, trade.InstrumentId, held, quantity.Value, entry.OccurredOn);
        }

        if (settled is { } received)
            sleeve.AddForeign(trade.Settlement!.Currency, received);
        else
            sleeve.Cash += trade.Price.TotalFor(quantity).Amount - fee;

        return null;
    }

    private static Error? ApplyCash(CashMovementDetails cash, Func<SubPortfolioId, Sleeve> sleeveOf)
    {
        var sign = cash.IsInflow ? 1m : -1m;

        if (cash.Settlement is { } settlement)
        {
            var parts = Split(settlement.Amount, [.. cash.Allocations.Select(allocation => allocation.Amount.Amount)]);

            for (var i = 0; i < cash.Allocations.Count; i++)
                sleeveOf(cash.Allocations[i].SubPortfolioId).AddForeign(settlement.Currency, sign * parts[i]);

            return null;
        }

        foreach (var allocation in cash.Allocations)
            sleeveOf(allocation.SubPortfolioId).Cash += sign * allocation.Amount.Amount;

        return null;
    }

    private static decimal[] Split(decimal amount, IReadOnlyList<decimal> weights)
    {
        var total = weights.Sum();
        var parts = weights.Select(weight => total > 0m ? decimal.Round(amount * weight / total, 8) : 0m).ToArray();
        parts[^1] += amount - parts.Sum();

        return parts;
    }

    private static Error? ApplyTransfer(Transaction entry, InternalTransferDetails transfer, Func<SubPortfolioId, Sleeve> sleeveOf)
    {
        var (from, to) = (sleeveOf(transfer.From), sleeveOf(transfer.To));

        if (transfer.Amount is { } amount)
        {
            from.Cash -= amount.Amount;
            to.Cash += amount.Amount;

            return null;
        }

        var instrumentId = transfer.InstrumentId!.Value;
        var quantity = transfer.Quantity!.Value;
        if (from.Take(instrumentId, quantity) is not { } moved)
            return TransactionErrors.NotEnoughUnits(transfer.From.Value, instrumentId, from.Units(instrumentId), quantity, entry.OccurredOn);

        var lots = to.LotsOf(instrumentId);
        lots.AddRange(moved);
        lots.Sort((left, right) => left.OpenedOn.CompareTo(right.OpenedOn));

        return null;
    }

    private sealed class Sleeve
    {
        private readonly Dictionary<Guid, List<Lot>> _lots = [];

        public decimal Cash { get; set; }

        public Dictionary<Currency, decimal> Foreign { get; } = [];

        public void AddForeign(Currency currency, decimal amount) => Foreign[currency] = Foreign.GetValueOrDefault(currency) + amount;

        public List<Lot> LotsOf(Guid instrumentId) =>
            _lots.TryGetValue(instrumentId, out var lots) ? lots : _lots[instrumentId] = [];

        public decimal Units(Guid instrumentId) => _lots.TryGetValue(instrumentId, out var lots) ? lots.Sum(lot => lot.Quantity) : 0m;

        public List<Lot>? Take(Guid instrumentId, decimal quantity)
        {
            if (Units(instrumentId) < quantity)
                return null;

            var lots = LotsOf(instrumentId);
            var taken = new List<Lot>();

            while (quantity > 0m)
            {
                var oldest = lots[0];
                var part = Math.Min(oldest.Quantity, quantity);
                taken.Add(oldest with { Quantity = part });
                quantity -= part;

                if (part == oldest.Quantity)
                    lots.RemoveAt(0);
                else
                    lots[0] = oldest with { Quantity = oldest.Quantity - part };
            }

            return taken;
        }

        public SubPortfolioHoldings Freeze(SubPortfolioId id, Currency currency)
        {
            var foreign = Foreign.Where(pair => decimal.Round(pair.Value, 2) != 0m).OrderBy(pair => pair.Key.Code);
            var held = _lots.Where(pair => pair.Value.Count > 0).OrderBy(pair => pair.Key);

            return new SubPortfolioHoldings(
                id,
                Money.Of(Cash, currency),
                [.. foreign.Select(pair => Money.Of(decimal.Round(pair.Value, 8), pair.Key))],
                [.. held.Select(pair => HoldingLineOf(pair.Key, pair.Value))]);
        }

        private static HoldingLine HoldingLineOf(Guid instrumentId, List<Lot> lots)
        {
            var virtualLots = lots.Select(lot => new VirtualLot(lot.OpenedBy, lot.OpenedOn, Quantity.Of(lot.Quantity), lot.UnitCost));

            return new HoldingLine(instrumentId, [.. virtualLots]);
        }
    }

    private sealed record Lot(TransactionId OpenedBy, DateOnly OpenedOn, decimal Quantity, decimal UnitCost);
}
