namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class SubPortfolioRebalancer
{
    public static SubPortfolioPlan Plan(SubPortfolioPlanInput input) => new PlanRun(input).Plan();

    private sealed class PlanRun(SubPortfolioPlanInput input)
    {
        private readonly StrategyTargets _strategy = input.Strategy;
        private readonly RebalancingPolicy _policy = input.Policy;
        private readonly List<Guid> _picks = [.. input.Strategy.Targets.Select(target => target.InstrumentId)];
        private readonly Dictionary<Guid, decimal> _weights =
            input.Strategy.Targets.ToDictionary(target => target.InstrumentId, target => target.Weight);
        private readonly Dictionary<Guid, SubPortfolioPosition> _holdings = input.Positions.ToDictionary(position => position.InstrumentId);
        private readonly List<string> _notes = [];
        private readonly List<PlannedOrder> _orders = [];

        public SubPortfolioPlan Plan()
        {
            var values = PricedPositions();
            var parkedBefore = TakeOutParked(values) + input.Cash;
            var totalBefore = values.Sum(entry => entry.Value) + parkedBefore;

            var proceeds = ExitDropped(values);

            var valueOf = values.ToDictionary(entry => entry.Id, entry => entry.Value);
            var kept = _picks.Where(valueOf.ContainsKey).ToDictionary(id => id, id => valueOf[id]);

            var investFraction = _strategy.InvestFraction.Value;
            var deployable = input.Contribution * investFraction + proceeds + (_strategy.MarketIsFavorable ? parkedBefore : 0m);
            var investableTotal = kept.Values.Sum() + deployable;

            var trims = TrimOverweight(kept, investableTotal);
            deployable += trims;

            var (bought, skipped) = BuyGaps(kept, investableTotal, deployable);

            var remaining = input.Cash + input.Contribution + proceeds + trims - bought;
            ParkOrUnpark(remaining);

            var sorted = _orders.OrderBy(order => order.Kind).ToList();

            var totalAfter = totalBefore + input.Contribution;
            var after = ValuesAfter(kept, sorted);
            var parkedAfter = totalAfter - after.Values.Sum();

            _notes.AddRange(Notes(input, _picks, investFraction, parkedBefore, skipped, Label));

            return new SubPortfolioPlan(
                _strategy.MarketIsFavorable,
                _strategy.InvestFraction,
                input.Contribution,
                input.CashInstrumentId,
                sorted,
                Allocation(values, after, _weights, _picks, input.CashInstrumentId, parkedBefore, parkedAfter, totalBefore, totalAfter),
                totalBefore,
                totalAfter,
                parkedBefore,
                parkedAfter,
                _notes);
        }

        private string Label(Guid id) => input.Labels.TryGetValue(id, out var label) ? label : id.ToString();

        private decimal? PriceOf(Guid id) => input.Prices.TryGetValue(id, out var price) && price > 0m ? price : null;

        private List<(Guid Id, decimal Value)> PricedPositions()
        {
            var values = new List<(Guid Id, decimal Value)>();

            foreach (var position in input.Positions)
            {
                if (PriceOf(position.InstrumentId) is { } latest)
                    values.Add((position.InstrumentId, position.Quantity.Value * latest));
                else
                    _notes.Add($"Немає ціни для {Label(position.InstrumentId)}: перевірте цю позицію вручну.");
            }

            return values;
        }

        private decimal TakeOutParked(List<(Guid Id, decimal Value)> values)
        {
            if (input.CashInstrumentId is not { } parkingId || values.FindIndex(entry => entry.Id == parkingId) is not (var parkedAt and >= 0))
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

                var reason =
                    $"вага вище цілі {PlanText.Percent(_weights[id])} більш ніж на {PlanText.Percent(_policy.TrimBand.Value)}: продати надлишок";
                var sold = AddOrder(OrderKind.Trim, id, excess, reason);

                kept[id] -= sold;
                trims += sold;
            }

            return trims;
        }

        private bool ShouldTrim(Guid id, decimal excess, decimal investableTotal) =>
            PriceOf(id) > _holdings[id].AverageCost
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
            if (input.CashInstrumentId is not { } cashEtf || PriceOf(cashEtf) is not { } price)
                return;

            if (remaining < 0m)
                Unpark(cashEtf, price, remaining);
            else if (remaining >= _policy.MinTrade)
                AddOrder(OrderKind.Park, cashEtf, remaining, "парковка: гроші, які цього місяця не інвестуються");
        }

        private void Unpark(Guid cashEtf, decimal price, decimal remaining)
        {
            var needed = -remaining / price;
            var wanted = _policy.AllowFractionalShares ? needed : Math.Ceiling(needed);
            var held = _holdings.TryGetValue(cashEtf, out var parked) ? parked.Quantity.Value : 0m;
            var shares = Math.Min(wanted, held);

            AddOrder(OrderKind.Unpark, cashEtf, shares * price, "запарковані кошти повертаються в роботу", shares);
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
            null => "не входить до списку ETF стратегії",
            { Reason: { Length: > 0 } reason } => $"випав із вибірки: {reason}",
            _ => "випав із вибірки",
        };

    private static string BuyReason(StrategyTargets strategy, Guid id, bool held, decimal weight)
    {
        var score = strategy.ScoreOf(id);
        var rank = score?.Rank is { } r ? $"ранг {r}" : "ранг —";
        var detail = score?.Momentum is { } momentum ? $"{rank}, моментум {PlanText.SignedPercent(momentum)}" : rank;
        var action = held ? "докупити до цілі" : "нова позиція, ціль";

        return $"{action} {PlanText.Percent(weight)} портфеля ({detail})";
    }

    private static IEnumerable<string> Notes(
        SubPortfolioPlanInput input, List<Guid> picks, decimal investFraction, decimal parkedBefore, List<Guid> skipped, Func<Guid, string> label)
    {
        var (contribution, currency, policy) = (input.Contribution, input.Currency, input.Policy);
        var parking = input.CashInstrumentId is { } cashEtf ? label(cashEtf) : "кеш на рахунку";

        if (contribution <= 0m)
            yield return "Внесок цього місяця дорівнює 0.";

        if (!input.Strategy.MarketIsFavorable)
            yield return $"Ринок слабкий: інвестуємо {PlanText.Percent(investFraction)} внеску " +
                         $"({PlanText.Money(contribution * investFraction, currency)}), " +
                         $"решту ({PlanText.Money(contribution * (1m - investFraction), currency)}) паркуємо в {parking}. " +
                         "Через стан ринку поточні позиції не продаємо.";
        else if (parkedBefore >= policy.MinTrade)
            yield return $"Ринок у нормі: запарковані кошти ({PlanText.Money(parkedBefore, currency)}) повертаються в роботу.";

        if (picks.Count == 0)
            yield return $"Жоден ETF не пройшов фільтри: усі гроші лишаються в {parking}.";
        else if (input.Strategy.Slots is { } slots && picks.Count < slots)
        {
            var empty = slots - picks.Count;
            yield return $"Знайдено {picks.Count} з {slots} кандидатів: {empty} порожн{(empty == 1 ? "ій слот" : "і слоти")} " +
                         $"({PlanText.Percent((decimal)empty / slots)} портфеля) лишаються в {parking} до наступного місяця.";
        }
        else if (input.Strategy.Slots is null && input.Strategy.TotalWeight < 1m)
            yield return $"Цілі стратегії займають {PlanText.Percent(input.Strategy.TotalWeight)} портфеля: решта лишається в {parking}.";

        if (skipped.Count > 0)
            yield return $"Пропущено дрібні покупки (менше {PlanText.Money(policy.MinTrade, currency)}): {string.Join(", ", skipped.Select(label))}.";
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
