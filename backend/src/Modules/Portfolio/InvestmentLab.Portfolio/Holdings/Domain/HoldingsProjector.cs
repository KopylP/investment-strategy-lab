using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Domain;

public static class HoldingsProjector
{
    public static Result<ProjectedHoldings> Project(InvestmentAccount account, IEnumerable<Transaction> journal, DateOnly? asOf = null)
    {
        var entries = journal.ToList();
        var reversed = entries.Where(entry => entry.ReversalOf is not null).Select(entry => entry.ReversalOf!.Value).ToHashSet();
        var effective = entries
            .Where(entry => entry.Kind != TransactionKind.Reversal && !reversed.Contains(entry.Id))
            .Where(entry => asOf is null || entry.OccurredOn <= asOf)
            .OrderBy(entry => entry.OccurredOn).ThenBy(entry => entry.RecordedAt).ThenBy(entry => entry.Id.Value)
            .ToList();

        var state = new Dictionary<SubPortfolioId, Sleeve>();
        Sleeve SleeveOf(SubPortfolioId id) => state.TryGetValue(id, out var sleeve) ? sleeve : state[id] = new Sleeve();

        foreach (var entry in effective)
        {
            var applied = entry.Details switch
            {
                TradeDetails trade => ApplyTrade(entry, trade, SleeveOf),
                CashMovementDetails cash => ApplyCash(cash, SleeveOf),
                InternalTransferDetails transfer => ApplyTransfer(entry, transfer, SleeveOf),
                _ => null,
            };

            if (applied is not null)
                return applied;
        }

        var subPortfolios = account.SubPortfolios
            .Where(sub => sub.IsActive || state.ContainsKey(sub.Id))
            .Select(sub => SleeveOf(sub.Id).Freeze(sub.Id, account.BaseCurrency))
            .ToList();

        var warnings = account.SubPortfolios
            .Where(sub => state.TryGetValue(sub.Id, out var sleeve) && sleeve.Cash < 0m)
            .Select(sub => $"Cash of '{sub.Name}' is {decimal.Round(state[sub.Id].Cash, 2)} {account.BaseCurrency}: record the contribution that paid for it.")
            .ToList();

        var lastDate = effective.Count > 0 ? effective[^1].OccurredOn : (DateOnly?)null;

        return new ProjectedHoldings(new AccountHoldings(account.Id, account.BaseCurrency, asOf ?? lastDate, subPortfolios), warnings);
    }

    private static Error? ApplyTrade(Transaction entry, TradeDetails trade, Func<SubPortfolioId, Sleeve> sleeveOf)
    {
        var fees = TradeAllocator.SplitFee(trade.Fee, trade.Allocations);
        var settlements = trade.Settlement is { } settlement
            ? Split(settlement.Amount, [.. trade.Allocations.Select(allocation => allocation.Quantity.Value)])
            : null;

        for (var i = 0; i < trade.Allocations.Count; i++)
        {
            var (subPortfolioId, quantity) = (trade.Allocations[i].SubPortfolioId, trade.Allocations[i].Quantity);
            var sleeve = sleeveOf(subPortfolioId);
            var gross = trade.Price.TotalFor(quantity).Amount;
            var settled = settlements?[i];

            if (trade.Side == TradeSide.Buy)
            {
                if (settled is { } paid)
                    sleeve.AddForeign(trade.Settlement!.Currency, -paid);
                else
                    sleeve.Cash -= gross + fees[i].Amount;

                sleeve.LotsOf(trade.InstrumentId).Add(new Lot(entry.Id, entry.OccurredOn, quantity.Value, trade.Price.Amount + fees[i].Amount / quantity.Value));
            }
            else
            {
                if (sleeve.Take(trade.InstrumentId, quantity.Value) is null)
                    return TransactionErrors.NotEnoughUnits(subPortfolioId.Value, trade.InstrumentId, sleeve.Units(trade.InstrumentId), quantity.Value, entry.OccurredOn);

                if (settled is { } received)
                    sleeve.AddForeign(trade.Settlement!.Currency, received);
                else
                    sleeve.Cash += gross - fees[i].Amount;
            }
        }

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

        public SubPortfolioHoldings Freeze(SubPortfolioId id, Currency currency) => new(
            id,
            Money.Of(Cash, currency),
            [.. Foreign.Where(pair => decimal.Round(pair.Value, 2) != 0m).OrderBy(pair => pair.Key.Code).Select(pair => Money.Of(decimal.Round(pair.Value, 8), pair.Key))],
            [.. _lots.Where(pair => pair.Value.Count > 0).OrderBy(pair => pair.Key).Select(pair => new HoldingLine(
                pair.Key,
                [.. pair.Value.Select(lot => new VirtualLot(lot.OpenedBy, lot.OpenedOn, Quantity.Of(lot.Quantity), lot.UnitCost))]))]);
    }

    private sealed record Lot(TransactionId OpenedBy, DateOnly OpenedOn, decimal Quantity, decimal UnitCost);
}
