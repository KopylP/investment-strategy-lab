namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class SubPortfolioRebalancer
{
    public static SubPortfolioPlan Plan(SubPortfolioSituation situation) => new PlanRun(situation).Plan();

    private sealed class PlanRun(SubPortfolioSituation situation)
    {
        private readonly StrategyTargets _strategy = situation.Strategy;
        private readonly RebalancingPolicy _policy = situation.Policy;
        private readonly List<Guid> _picks = [.. situation.Strategy.Targets.Select(target => target.InstrumentId)];
        private readonly Dictionary<Guid, decimal> _weights =
            situation.Strategy.Targets.ToDictionary(target => target.InstrumentId, target => target.Weight);
        private readonly Dictionary<Guid, SubPortfolioPosition> _holdings = situation.Positions.ToDictionary(position => position.InstrumentId);
        private readonly List<string> _notes = [];
        private readonly List<PlannedOrder> _orders = [];

        public SubPortfolioPlan Plan()
        {
            var values = PricedPositions();
            var parkedBefore = TakeOutParked(values) + situation.Cash;
            var totalBefore = values.Sum(entry => entry.Value) + parkedBefore;

            var proceeds = ExitDropped(values);

            var valueOf = values.ToDictionary(entry => entry.Id, entry => entry.Value);
            var kept = _picks.Where(valueOf.ContainsKey).ToDictionary(id => id, id => valueOf[id]);

            var investFraction = _strategy.InvestFraction.Value;
            var deployable = situation.Contribution * investFraction + proceeds + (_strategy.MarketIsFavorable ? parkedBefore : 0m);
            var investableTotal = kept.Values.Sum() + deployable;

            var trims = TrimOverweight(kept, investableTotal);
            deployable += trims;

            var (bought, skipped) = BuyGaps(kept, investableTotal, deployable);

            var remaining = situation.Cash + situation.Contribution + proceeds + trims - bought;
            ParkOrUnpark(remaining);

            var sorted = _orders.OrderBy(order => order.Kind).ToList();

            var totalAfter = totalBefore + situation.Contribution;
            var after = ValuesAfter(kept, sorted);
            var parkedAfter = totalAfter - after.Values.Sum();

            _notes.AddRange(Notes(situation, _picks, investFraction, parkedBefore, skipped, Label));

            return new SubPortfolioPlan(
                _strategy.MarketIsFavorable,
                _strategy.InvestFraction,
                situation.Contribution,
                situation.CashInstrumentId,
                sorted,
                Allocation(values, after, _weights, _picks, situation.CashInstrumentId, parkedBefore, parkedAfter, totalBefore, totalAfter),
                totalBefore,
                totalAfter,
                parkedBefore,
                parkedAfter,
                _notes);
        }

        private string Label(Guid id) => situation.Labels.TryGetValue(id, out var label) ? label : id.ToString();

        private decimal? PriceOf(Guid id) => situation.Prices.TryGetValue(id, out var price) && price > 0m ? price : null;

        private List<(Guid Id, decimal Value)> PricedPositions()
        {
            var values = new List<(Guid Id, decimal Value)>();

            foreach (var position in situation.Positions)
            {
                if (PriceOf(position.InstrumentId) is { } latest)
                    values.Add((position.InstrumentId, position.Quantity.Value * latest));
                else
                    _notes.Add(Message.Of("report.note.no_price", new { label = Label(position.InstrumentId) }));
            }

            return values;
        }

        private decimal TakeOutParked(List<(Guid Id, decimal Value)> values)
        {
            if (situation.CashInstrumentId is not { } parkingId || values.FindIndex(entry => entry.Id == parkingId) is not (var parkedAt and >= 0))
                return 0m;

            var parked = values[parkedAt].Value;
            values.RemoveAt(parkedAt);

            return parked;
        }

        private decimal AddOrder(OrderKind kind, Guid id, decimal amount, string reason, decimal? quantity = null)
        {
            if (PriceOf(id) is not { } latest || amount <= 0m)
                return 0m;

            if (quantity is not { } shares)
            {
                shares = amount / latest;

                if (!_policy.AllowFractionalShares)
                {
                    shares = Math.Floor(shares);
                    amount = shares * latest;
                }
            }

            if (shares <= 0m)
                return 0m;

            _orders.Add(new PlannedOrder(kind, id, shares, latest, amount, reason));

            return amount;
        }

        private decimal ExitDropped(List<(Guid Id, decimal Value)> values)
        {
            var proceeds = 0m;

            foreach (var (id, value) in values.Where(entry => !_weights.ContainsKey(entry.Id)))
                proceeds += AddOrder(OrderKind.Exit, id, value, ExitReason(_strategy, id), _holdings[id].Quantity.Value);

            return proceeds;
        }

        private decimal TrimOverweight(Dictionary<Guid, decimal> kept, decimal investableTotal)
        {
            var trims = 0m;

            foreach (var id in kept.Keys.ToList())
            {
                var excess = kept[id] - investableTotal * _weights[id];

                if (!ShouldTrim(id, excess, investableTotal))
                    continue;

                var reason = Message.Of("report.reason.trim", new { target = _weights[id], band = _policy.TrimBand.Value });
                var sold = AddOrder(OrderKind.Trim, id, excess, reason);

                kept[id] -= sold;
                trims += sold;
            }

            return trims;
        }

        private bool ShouldTrim(Guid id, decimal excess, decimal investableTotal) =>
            !_policy.LevelByPurchasesOnly
            && PriceOf(id) > _holdings[id].AverageCost
            && excess > _policy.TrimBand.Value * investableTotal
            && excess >= _policy.MinTrade;

        private (decimal Bought, List<Guid> Skipped) BuyGaps(Dictionary<Guid, decimal> kept, decimal investableTotal, decimal deployable)
        {
            var gaps = _picks.ToDictionary(id => id, id => Math.Max(0m, investableTotal * _weights[id] - kept.GetValueOrDefault(id)));
            var totalGap = gaps.Values.Sum();
            var scale = totalGap > 0m ? Math.Min(1m, deployable / totalGap) : 0m;

            var skipped = new List<Guid>();
            var bought = 0m;

            foreach (var id in _picks)
            {
                var amount = gaps[id] * scale;

                if (amount <= 0m)
                    continue;

                if (amount < _policy.MinTrade)
                    skipped.Add(id);
                else
                    bought += AddOrder(OrderKind.Buy, id, amount, BuyReason(_strategy, id, kept.ContainsKey(id), _weights[id]));
            }

            return (bought, skipped);
        }

        private void ParkOrUnpark(decimal remaining)
        {
            if (situation.CashInstrumentId is not { } cashEtf || PriceOf(cashEtf) is not { } price)
                return;

            if (remaining < 0m)
                Unpark(cashEtf, price, remaining);
            else if (remaining >= _policy.MinTrade)
                AddOrder(OrderKind.Park, cashEtf, remaining, Message.Of("report.reason.park"));
        }

        private void Unpark(Guid cashEtf, decimal price, decimal remaining)
        {
            var needed = -remaining / price;
            var wanted = _policy.AllowFractionalShares ? needed : Math.Ceiling(needed);
            var held = _holdings.TryGetValue(cashEtf, out var parked) ? parked.Quantity.Value : 0m;
            var shares = Math.Min(wanted, held);

            AddOrder(OrderKind.Unpark, cashEtf, shares * price, Message.Of("report.reason.unpark"), shares);
        }

        private Dictionary<Guid, decimal> ValuesAfter(Dictionary<Guid, decimal> kept, List<PlannedOrder> sorted)
        {
            var after = _picks.ToDictionary(id => id, id => kept.GetValueOrDefault(id));

            foreach (var order in sorted.Where(order => order.Kind == OrderKind.Buy))
                after[order.InstrumentId] += order.Amount;

            return after;
        }
    }

    private static string ExitReason(StrategyTargets strategy, Guid id) =>
        strategy.ScoreOf(id) switch
        {
            null => Message.Of("report.reason.exit_not_in_universe"),
            { Reason: { Length: > 0 } reason } => Message.Of("report.reason.exit_dropped_because", new { reason }),
            _ => Message.Of("report.reason.exit_dropped"),
        };

    private static string BuyReason(StrategyTargets strategy, Guid id, bool held, decimal weight)
    {
        var score = strategy.ScoreOf(id);
        var rank = score?.Rank is { } known ? Message.Of("report.reason.rank", new { rank = known }) : Message.Of("report.reason.rank_unknown");
        var detail = score?.Momentum is { } momentum ? Message.Of("report.reason.rank_momentum", new { rank, momentum }) : rank;
        var code = held ? "report.reason.buy_more" : "report.reason.buy_new";

        return Message.Of(code, new { weight, detail });
    }

    private static IEnumerable<string> Notes(
        SubPortfolioSituation situation, List<Guid> picks, decimal investFraction, decimal parkedBefore, List<Guid> skipped, Func<Guid, string> label)
    {
        var (contribution, currency, policy) = (situation.Contribution, situation.Currency.Code, situation.Policy);
        var parking = situation.CashInstrumentId is { } cashEtf ? label(cashEtf) : Message.Of("report.parking.cash");

        if (contribution <= 0m)
            yield return Message.Of("report.note.zero_contribution");

        if (!situation.Strategy.MarketIsFavorable)
            yield return Message.Of("report.note.weak_market", new
            {
                fraction = investFraction,
                invested = contribution * investFraction,
                rest = contribution * (1m - investFraction),
                currency,
                parking,
            });
        else if (parkedBefore >= policy.MinTrade)
            yield return Message.Of("report.note.market_recovered", new { parked = parkedBefore, currency });

        if (picks.Count == 0)
            yield return Message.Of("report.note.nothing_picked", new { parking });
        else if (situation.Strategy.Slots is { } slots && picks.Count < slots)
        {
            var empty = slots - picks.Count;
            yield return Message.Of("report.note.empty_slots", new { found = picks.Count, slots, empty, share = (decimal)empty / slots, parking });
        }
        else if (situation.Strategy.Slots is null && situation.Strategy.TotalWeight < 1m)
            yield return Message.Of("report.note.partial_targets", new { total = situation.Strategy.TotalWeight, parking });

        if (skipped.Count > 0)
            yield return Message.Of("report.note.small_buys_skipped", new { minimum = policy.MinTrade, currency, labels = string.Join(", ", skipped.Select(label)) });
    }

    private static List<AllocationRow> Allocation(
        List<(Guid Id, decimal Value)> values,
        Dictionary<Guid, decimal> after,
        Dictionary<Guid, decimal> weights,
        List<Guid> picks,
        Guid? cashEtf,
        decimal parkedBefore,
        decimal parkedAfter,
        decimal totalBefore,
        decimal totalAfter)
    {
        var valueOf = values.ToDictionary(entry => entry.Id, entry => entry.Value);

        var rows = picks.Select(id => PickRow(id, valueOf.GetValueOrDefault(id), after[id], weights[id], totalBefore, totalAfter)).ToList();

        var exits = values.Where(entry => !weights.ContainsKey(entry.Id));
        rows.AddRange(exits.Select(entry => new AllocationRow(
            entry.Id,
            AllocationRole.Exit,
            entry.Value,
            0m,
            Weight(entry.Value, totalBefore),
            0m,
            0m)));

        rows.Add(new AllocationRow(
            cashEtf,
            AllocationRole.Cash,
            parkedBefore,
            parkedAfter,
            Weight(parkedBefore, totalBefore),
            Weight(parkedAfter, totalAfter),
            null));

        return rows;
    }

    private static AllocationRow PickRow(Guid id, decimal before, decimal after, decimal weight, decimal totalBefore, decimal totalAfter) => new(
        id,
        AllocationRole.Pick,
        before,
        after,
        Weight(before, totalBefore),
        Weight(after, totalAfter),
        weight);

    private static decimal Weight(decimal value, decimal total) => total > 0m ? value / total : 0m;
}
