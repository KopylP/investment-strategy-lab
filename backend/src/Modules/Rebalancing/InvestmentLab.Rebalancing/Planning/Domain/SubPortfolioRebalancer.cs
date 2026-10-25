namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <param name="Cash">Free cash of the sub-portfolio (not counting the cash ETF).</param>
/// <param name="Prices">Latest price per instrument; instruments without a price cannot be traded.</param>
/// <param name="Labels">Short names (tickers) for notes; the instrument id is used when missing.</param>
public sealed record SubPortfolioPlanInput(
    StrategyTargets Strategy,
    IReadOnlyList<SubPortfolioPosition> Positions,
    decimal Cash,
    decimal Contribution,
    IReadOnlyDictionary<Guid, decimal> Prices,
    Guid? CashInstrumentId,
    RebalancingPolicy Policy,
    Currency Currency,
    IReadOnlyDictionary<Guid, string> Labels);

/// <summary>
/// Turns a strategy decision and what a sub-portfolio holds into the month's orders — the rules of <c>plan.py</c>:
/// <list type="number">
/// <item>Sell every position the strategy no longer selects.</item>
/// <item>Favorable market: invest the whole contribution plus parked cash. Weak market: invest the strategy's invest
/// fraction of the contribution and park the rest. Sale proceeds are always reinvested.</item>
/// <item>Each pick targets its weight of the invested portfolio; unfilled weight stays parked.</item>
/// <item>Top up underweight picks. Trim a pick only in profit and more than the trim band over target.</item>
/// <item>Orders below the minimum trade are skipped; what is not invested goes to the cash ETF.</item>
/// </list>
/// </summary>
public static class SubPortfolioRebalancer
{
    public static SubPortfolioPlan Plan(SubPortfolioPlanInput input)
    {
        var (strategy, policy) = (input.Strategy, input.Policy);
        var picks = strategy.Targets.Select(target => target.InstrumentId).ToList();
        var weights = strategy.Targets.ToDictionary(target => target.InstrumentId, target => target.Weight);
        var holdings = input.Positions.ToDictionary(position => position.InstrumentId);
        var cashEtf = input.CashInstrumentId;
        var notes = new List<string>();
        var orders = new List<PlannedOrder>();

        string Label(Guid id) => input.Labels.TryGetValue(id, out var label) ? label : id.ToString();
        decimal? PriceOf(Guid id) => input.Prices.TryGetValue(id, out var price) && price > 0m ? price : null;

        var values = new List<(Guid Id, decimal Value)>();
        foreach (var position in input.Positions)
        {
            if (PriceOf(position.InstrumentId) is not { } latest)
            {
                notes.Add($"Немає ціни для {Label(position.InstrumentId)}: перевірте цю позицію вручну.");
                continue;
            }
            values.Add((position.InstrumentId, position.Quantity.Value * latest));
        }

        var cashEtfValue = 0m;
        if (cashEtf is { } parkingId && values.FindIndex(entry => entry.Id == parkingId) is var parkedAt and >= 0)
        {
            cashEtfValue = values[parkedAt].Value;
            values.RemoveAt(parkedAt);
        }
        var parkedBefore = cashEtfValue + input.Cash;
        var totalBefore = values.Sum(entry => entry.Value) + parkedBefore;

        decimal AddOrder(OrderKind kind, Guid id, decimal amount, string reason, decimal? quantity = null)
        {
            if (PriceOf(id) is not { } latest || amount <= 0m)
                return 0m;
            if (quantity is not { } shares)
            {
                shares = amount / latest;
                if (!policy.AllowFractionalShares)
                {
                    shares = Math.Floor(shares);
                    amount = shares * latest;
                }
            }
            if (shares <= 0m)
                return 0m;
            orders.Add(new PlannedOrder(kind, id, shares, latest, amount, reason));
            return amount;
        }

        // 1. Exit positions that dropped out of the selection.
        var proceeds = 0m;
        foreach (var (id, value) in values.Where(entry => !weights.ContainsKey(entry.Id)))
            proceeds += AddOrder(OrderKind.Exit, id, value, ExitReason(strategy, id), holdings[id].Quantity.Value);

        var valueOf = values.ToDictionary(entry => entry.Id, entry => entry.Value);
        var kept = picks.Where(valueOf.ContainsKey).ToDictionary(id => id, id => valueOf[id]);

        // 2. Money to invest this month.
        var investFraction = strategy.InvestFraction.Value;
        var deployable = input.Contribution * investFraction + proceeds + (strategy.MarketIsFavorable ? parkedBefore : 0m);
        var investableTotal = kept.Values.Sum() + deployable;
        decimal TargetValue(Guid id) => investableTotal * weights[id];

        // 3. Trim winners that grew far above target.
        var trims = 0m;
        foreach (var id in kept.Keys.ToList())
        {
            var excess = kept[id] - TargetValue(id);
            var inProfit = PriceOf(id) > holdings[id].AverageCost;
            if (inProfit && excess > policy.TrimBand.Value * investableTotal && excess >= policy.MinTrade)
            {
                var sold = AddOrder(OrderKind.Trim, id, excess,
                    $"вага вище цілі {PlanText.Percent(weights[id])} більш ніж на {PlanText.Percent(policy.TrimBand.Value)}: продати надлишок");
                kept[id] -= sold;
                trims += sold;
            }
        }
        deployable += trims;

        // 4. Top up underweight picks, scaled down if money is short.
        var gaps = picks.ToDictionary(id => id, id => Math.Max(0m, TargetValue(id) - kept.GetValueOrDefault(id)));
        var totalGap = gaps.Values.Sum();
        var scale = totalGap > 0m ? Math.Min(1m, deployable / totalGap) : 0m;
        var skipped = new List<Guid>();
        var bought = 0m;
        foreach (var id in picks)
        {
            var amount = gaps[id] * scale;
            if (amount <= 0m)
                continue;
            if (amount < policy.MinTrade)
            {
                skipped.Add(id);
                continue;
            }
            bought += AddOrder(OrderKind.Buy, id, amount, BuyReason(strategy, id, kept.ContainsKey(id), weights[id]));
        }

        // 5. Whatever was not invested moves to (or comes from) the cash ETF.
        var remaining = input.Cash + input.Contribution + proceeds + trims - bought;
        if (remaining < 0m && cashEtf is { } unparkId && PriceOf(unparkId) is { } unparkPrice)
        {
            var needed = -remaining / unparkPrice;
            var shares = policy.AllowFractionalShares ? needed : Math.Ceiling(needed);
            shares = Math.Min(shares, holdings.TryGetValue(unparkId, out var parked) ? parked.Quantity.Value : 0m);
            AddOrder(OrderKind.Unpark, unparkId, shares * unparkPrice, "запарковані кошти повертаються в роботу", shares);
        }
        else if (remaining >= policy.MinTrade && cashEtf is { } parkId && PriceOf(parkId) is not null)
        {
            AddOrder(OrderKind.Park, parkId, remaining, "парковка: гроші, які цього місяця не інвестуються");
        }

        var sorted = orders.OrderBy(order => order.Kind).ToList();

        var totalAfter = totalBefore + input.Contribution;
        var after = picks.ToDictionary(id => id, id => kept.GetValueOrDefault(id));
        foreach (var order in sorted.Where(order => order.Kind == OrderKind.Buy))
            after[order.InstrumentId] += order.Amount;
        var parkedAfter = totalAfter - after.Values.Sum();

        notes.AddRange(Notes(input, picks, investFraction, parkedBefore, skipped, Label));

        return new SubPortfolioPlan(
            strategy.MarketIsFavorable,
            strategy.InvestFraction,
            input.Contribution,
            cashEtf,
            sorted,
            Allocation(values, after, weights, picks, cashEtf, parkedBefore, parkedAfter, totalBefore, totalAfter),
            totalBefore,
            totalAfter,
            parkedBefore,
            parkedAfter,
            notes);
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
            yield return $"Ринок слабкий: інвестуємо {PlanText.Percent(investFraction)} внеску ({PlanText.Money(contribution * investFraction, currency)}), " +
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
        static decimal Weight(decimal value, decimal total) => total > 0m ? value / total : 0m;
        var valueOf = values.ToDictionary(entry => entry.Id, entry => entry.Value);

        var rows = picks.Select(id => new AllocationRow(
                id, AllocationRole.Pick, valueOf.GetValueOrDefault(id), after[id],
                Weight(valueOf.GetValueOrDefault(id), totalBefore), Weight(after[id], totalAfter), weights[id]))
            .ToList();
        rows.AddRange(values.Where(entry => !weights.ContainsKey(entry.Id)).Select(entry => new AllocationRow(
            entry.Id, AllocationRole.Exit, entry.Value, 0m, Weight(entry.Value, totalBefore), 0m, 0m)));
        rows.Add(new AllocationRow(
            cashEtf, AllocationRole.Cash, parkedBefore, parkedAfter, Weight(parkedBefore, totalBefore), Weight(parkedAfter, totalAfter), null));
        return rows;
    }
}
