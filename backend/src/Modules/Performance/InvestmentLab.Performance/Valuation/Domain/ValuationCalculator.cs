namespace InvestmentLab.Performance.Valuation.Domain;

public static class ValuationCalculator
{
    public static IReadOnlyList<Snapshot> Build(IReadOnlyList<Movement> movements, IReadOnlyList<DateOnly> days, PriceOn priceOn, FxOn? fxOn = null)
    {
        var ordered = movements.OrderBy(movement => movement.Date).ThenBy(movement => movement.Sequence).ToList();
        var subs = ordered.Select(movement => movement.SubPortfolioId).Distinct().ToList();
        var quantities = new Dictionary<(Guid Sub, Guid Instrument), decimal>();
        var cash = subs.ToDictionary(sub => sub, _ => 0m);
        var lastTradePrice = new Dictionary<Guid, decimal>();
        var foreign = new Dictionary<(Guid Sub, string Currency), decimal>();
        var lastRate = new Dictionary<string, decimal>();
        var costs = new CostBook();

        decimal Rate(string currency, DateOnly day)
        {
            if (fxOn?.Invoke(currency, day) is { } rate)
                lastRate[currency] = rate;

            return lastRate.GetValueOrDefault(currency);
        }

        var state = subs.Cast<Guid?>().Append(null).ToDictionary(sub => sub ?? Guid.Empty, _ => new Chain());

        decimal Price(Guid instrument, DateOnly day) => priceOn(instrument, day) ?? lastTradePrice.GetValueOrDefault(instrument);

        var snapshots = new List<Snapshot>();
        var next = 0;

        foreach (var day in days)
        {
            var flows = subs.ToDictionary(sub => sub, _ => 0m);
            var accountFlow = 0m;

            for (; next < ordered.Count && ordered[next].Date <= day; next++)
            {
                var movement = ordered[next];
                costs.Apply(movement);

                if (movement.InstrumentId is { } instrument)
                {
                    var key = (movement.SubPortfolioId, instrument);
                    quantities[key] = quantities.GetValueOrDefault(key) + movement.QuantityDelta;

                    if (movement.TradePrice is { } tradePrice)
                        lastTradePrice[instrument] = tradePrice;

                    if (movement.TransferQuantity != 0m)
                        flows[movement.SubPortfolioId] += movement.TransferQuantity * Price(instrument, day);
                }

                if (movement.CashCurrency is { } currency)
                    foreign[(movement.SubPortfolioId, currency)] = foreign.GetValueOrDefault((movement.SubPortfolioId, currency)) + movement.CashDelta;
                else
                    cash[movement.SubPortfolioId] += movement.CashDelta;

                flows[movement.SubPortfolioId] += movement.ExternalFlow;
                accountFlow += movement.ExternalFlow;
            }

            var accountValue = 0m;
            var accountCash = 0m;
            var accountCost = 0m;

            foreach (var sub in subs)
            {
                var subCash = cash[sub] + foreign.Where(entry => entry.Key.Sub == sub && entry.Value != 0m)
                    .Sum(entry => entry.Value * Rate(entry.Key.Currency, day));
                var value = subCash + quantities.Where(entry => entry.Key.Sub == sub && entry.Value != 0m)
                    .Sum(entry => entry.Value * Price(entry.Key.Instrument, day));
                accountValue += value;
                accountCash += subCash;
                var subCost = costs.Of(sub);
                accountCost += subCost;
                snapshots.Add(state[sub].Next(day, sub, value, subCash, flows[sub], subCost));
            }

            snapshots.Add(state[Guid.Empty].Next(day, null, accountValue, accountCash, accountFlow, accountCost));
        }

        return snapshots;
    }

    private sealed class Chain
    {
        private decimal _previousValue;
        private decimal _index = 1m;
        private decimal _contributions;

        public Snapshot Next(DateOnly day, Guid? sub, decimal value, decimal cash, decimal flow, decimal costBasis)
        {
            var start = _previousValue + flow;
            if (start > 0m)
                _index *= value / start;

            _contributions += flow;
            _previousValue = value;

            return new Snapshot(day, sub, decimal.Round(value, 2), decimal.Round(cash, 2), decimal.Round(flow, 2), decimal.Round(_contributions, 2), decimal.Round(_index, 8), decimal.Round(costBasis, 2));
        }
    }

    private sealed class CostBook
    {
        private readonly Dictionary<(Guid Sub, Guid Instrument), List<Lot>> _lots = [];
        private readonly Dictionary<Guid, List<Lot>> _inTransfer = [];

        public void Apply(Movement movement)
        {
            if (movement.InstrumentId is not { } instrument || movement.QuantityDelta == 0m)
                return;

            var key = (movement.SubPortfolioId, instrument);

            if (movement.TransferQuantity < 0m)
                InTransfer(instrument).AddRange(Take(LotsOf(key), -movement.QuantityDelta));
            else if (movement.TransferQuantity > 0m)
            {
                var lots = LotsOf(key);
                lots.AddRange(Take(InTransfer(instrument), movement.QuantityDelta));
                lots.Sort((left, right) => left.OpenedOn.CompareTo(right.OpenedOn));
            }
            else if (movement.QuantityDelta > 0m)
                LotsOf(key).Add(new Lot(movement.Date, movement.QuantityDelta, movement.UnitCost ?? movement.TradePrice ?? 0m));
            else
                Take(LotsOf(key), -movement.QuantityDelta);
        }

        public decimal Of(Guid sub) => _lots.Where(entry => entry.Key.Sub == sub).Sum(entry => entry.Value.Sum(lot => lot.Quantity * lot.UnitCost));

        private List<Lot> LotsOf((Guid Sub, Guid Instrument) key) => _lots.TryGetValue(key, out var lots) ? lots : _lots[key] = [];

        private List<Lot> InTransfer(Guid instrument) => _inTransfer.TryGetValue(instrument, out var lots) ? lots : _inTransfer[instrument] = [];

        private static List<Lot> Take(List<Lot> lots, decimal quantity)
        {
            var taken = new List<Lot>();

            while (quantity > 0m && lots.Count > 0)
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

        private sealed record Lot(DateOnly OpenedOn, decimal Quantity, decimal UnitCost);
    }
}
