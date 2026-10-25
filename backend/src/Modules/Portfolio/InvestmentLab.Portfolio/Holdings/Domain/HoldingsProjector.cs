using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Domain;

/// <summary>
/// Replays the journal in date order into holdings per sub-portfolio. A reversed entry and its reversal are skipped,
/// so holdings look as if the entry never happened. Fails when, at any date, a sub-portfolio gives away units it does not hold.
/// Sales and transfers take units first-in, first-out.
/// </summary>
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

        for (var i = 0; i < trade.Allocations.Count; i++)
        {
            var (subPortfolioId, quantity) = (trade.Allocations[i].SubPortfolioId, trade.Allocations[i].Quantity);
            var sleeve = sleeveOf(subPortfolioId);
            var gross = trade.Price.TotalFor(quantity).Amount;

            if (trade.Side == TradeSide.Buy)
            {
                // Cash pays the rounded total; the lot keeps the exact unit price so the average cost equals the trade price.
                sleeve.Cash -= gross + fees[i].Amount;
                sleeve.LotsOf(trade.InstrumentId).Add(new Lot(entry.Id, entry.OccurredOn, quantity.Value, trade.Price.Amount + fees[i].Amount / quantity.Value));
            }
            else
            {
                if (sleeve.Take(trade.InstrumentId, quantity.Value) is null)
                    return TransactionErrors.NotEnoughUnits(subPortfolioId.Value, trade.InstrumentId, sleeve.Units(trade.InstrumentId), quantity.Value, entry.OccurredOn);
                sleeve.Cash += gross - fees[i].Amount;
            }
        }

        return null;
    }

    private static Error? ApplyCash(CashMovementDetails cash, Func<SubPortfolioId, Sleeve> sleeveOf)
    {
        foreach (var allocation in cash.Allocations)
            sleeveOf(allocation.SubPortfolioId).Cash += cash.IsInflow ? allocation.Amount.Amount : -allocation.Amount.Amount;

        return null;
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

        public List<Lot> LotsOf(Guid instrumentId) =>
            _lots.TryGetValue(instrumentId, out var lots) ? lots : _lots[instrumentId] = [];

        public decimal Units(Guid instrumentId) => _lots.TryGetValue(instrumentId, out var lots) ? lots.Sum(lot => lot.Quantity) : 0m;

        /// <summary>Removes units first-in, first-out and returns them as lots; null when not enough are held.</summary>
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
            [.. _lots.Where(pair => pair.Value.Count > 0).OrderBy(pair => pair.Key).Select(pair => new HoldingLine(
                pair.Key,
                [.. pair.Value.Select(lot => new VirtualLot(lot.OpenedBy, lot.OpenedOn, Quantity.Of(lot.Quantity), lot.UnitCost))]))]);
    }

    private sealed record Lot(TransactionId OpenedBy, DateOnly OpenedOn, decimal Quantity, decimal UnitCost);
}
