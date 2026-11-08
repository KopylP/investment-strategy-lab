namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class SubPortfolioRebalancer
{
    private const decimal Cent = 0.01m;

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
        private readonly SkippedTrades _skipped = new();

        public SubPortfolioPlan Plan()
        {
            var values = PricedPositions();
            var parkedBefore = TakeOutParked(values) + situation.Cash;
            var totalBefore = values.Sum(entry => entry.Value) + parkedBefore;
            var totalAfter = totalBefore + situation.Contribution;

            var proceeds = ExitDropped(values);
            var kept = KeptPicks(values);

            var trims = TrimOverweight(kept, totalAfter);
            var deployable = Deployable(proceeds, parkedBefore) + trims;
            var bought = BuyGaps(kept, totalAfter, deployable);

            var remaining = situation.Cash + situation.Contribution + proceeds + trims - bought;
            ParkOrUnpark(remaining);

            var sorted = _orders.OrderBy(order => order.Kind).ToList();
            var after = ValuesAfter(kept, sorted);
            var parkedAfter = totalAfter - after.Values.Sum();

            _notes.AddRange(Notes(parkedBefore));

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

        private Dictionary<Guid, decimal> KeptPicks(List<(Guid Id, decimal Value)> values)
        {
            var valueOf = values.ToDictionary(entry => entry.Id, entry => entry.Value);

            return _picks.Where(valueOf.ContainsKey).ToDictionary(id => id, id => valueOf[id]);
        }

        private decimal Deployable(decimal proceeds, decimal parkedBefore)
        {
            var investedContribution = situation.Contribution * _strategy.InvestFraction.Value;
            var unparked = _strategy.MarketIsFavorable ? parkedBefore : 0m;

            return investedContribution + proceeds + unparked;
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

        private decimal TrimOverweight(Dictionary<Guid, decimal> kept, decimal totalAfter)
        {
            var trims = 0m;

            foreach (var id in kept.Keys.ToList())
            {
                var sold = TrimOne(id, kept[id], totalAfter);

                kept[id] -= sold;
                trims += sold;
            }

            return trims;
        }

        private decimal TrimOne(Guid id, decimal value, decimal totalAfter)
        {
            var excess = value - totalAfter * _weights[id];

            if (!IsBeyondTrimBand(excess, totalAfter))
                return 0m;

            if (_policy.LevelByPurchasesOnly)
            {
                _skipped.TrimsBySetting.Add(id);
                return 0m;
            }

            if (!IsInProfit(id))
            {
                _skipped.TrimsAtLoss.Add(id);
                return 0m;
            }

            var reason = Message.Of("report.reason.trim", new { weight = value / totalAfter, target = _weights[id], band = _policy.TrimBand.Value });

            return AddOrder(OrderKind.Trim, id, excess, reason);
        }

        private bool IsBeyondTrimBand(decimal excess, decimal totalAfter) =>
            excess > _policy.TrimBand.Value * totalAfter && excess >= _policy.MinTrade;

        private bool IsInProfit(Guid id) => _holdings[id].AverageCost is { } cost && PriceOf(id) > cost;

        private decimal BuyGaps(Dictionary<Guid, decimal> kept, decimal totalAfter, decimal deployable)
        {
            var gaps = _picks.ToDictionary(id => id, id => GapOf(id, kept, totalAfter));
            var scale = AffordableShareOfGaps(gaps.Values.Sum(), deployable);
            var bought = 0m;

            foreach (var id in _picks)
                bought += BuyOne(id, gaps[id] * scale, kept.ContainsKey(id));

            return bought;
        }

        private decimal GapOf(Guid id, Dictionary<Guid, decimal> kept, decimal totalAfter)
        {
            var shortfall = totalAfter * _weights[id] - kept.GetValueOrDefault(id);

            return shortfall < Cent ? 0m : shortfall;
        }

        private static decimal AffordableShareOfGaps(decimal totalGap, decimal deployable) =>
            totalGap > 0m ? Math.Min(1m, deployable / totalGap) : 0m;

        private decimal BuyOne(Guid id, decimal amount, bool held)
        {
            if (amount <= 0m)
                return 0m;

            if (amount < _policy.MinTrade)
            {
                _skipped.SmallBuys.Add(id);
                return 0m;
            }

            if (PriceOf(id) is not { } price)
            {
                _skipped.UnpricedBuys.Add(id);
                return 0m;
            }

            if (!_policy.AllowFractionalShares && amount < price)
            {
                _skipped.BuysBelowOneShare.Add(id);
                return 0m;
            }

            return AddOrder(OrderKind.Buy, id, amount, BuyReason(_strategy, id, held, _weights[id]));
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

        private IEnumerable<string> Notes(decimal parkedBefore)
        {
            var parking = situation.CashInstrumentId is { } cashEtf ? Label(cashEtf) : Message.Of("report.parking.cash");
            var notes = new PlanNotes(situation, parking, Label);

            return
            [
                .. notes.OfContribution(),
                .. notes.OfMarket(parkedBefore),
                .. notes.OfTargets(_picks.Count),
                .. notes.OfSkipped(_skipped),
            ];
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
