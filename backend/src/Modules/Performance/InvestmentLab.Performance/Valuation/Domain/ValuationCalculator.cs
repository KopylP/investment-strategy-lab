namespace InvestmentLab.Performance.Valuation.Domain;

public static class ValuationCalculator
{
    public static Result<IReadOnlyList<Snapshot>> Build(
        IReadOnlyList<Movement> movements, IReadOnlyList<DateOnly> days, PriceOn priceOn, FxOn? fxOn = null)
    {
        var ordered = movements.OrderBy(movement => movement.Date).ThenBy(movement => movement.Sequence).ToList();
        var subs = ordered.Select(movement => movement.SubPortfolioId).Distinct().ToList();

        var book = new PositionBook(subs, priceOn, fxOn);
        var chains = subs.ToDictionary(sub => sub, _ => new Chain());
        var accountChain = new Chain();

        var snapshots = new List<Snapshot>();
        var next = 0;

        foreach (var day in days)
        {
            var flows = new DayFlows(subs);

            for (; next < ordered.Count && ordered[next].Date <= day; next++)
                book.Apply(ordered[next], day, flows);

            var account = new AccountTotals();

            foreach (var sub in subs)
            {
                var valuation = book.ValuationOf(sub, day);
                if (valuation.IsFailure)
                    return valuation.Error;

                var (value, cash, cost) = valuation.Value;

                account.Add(value, cash, cost);
                snapshots.Add(chains[sub].Next(day, sub, value, cash, flows.OfSub[sub], cost));
            }

            snapshots.Add(accountChain.Next(day, null, account.Value, account.Cash, flows.OfAccount, account.Cost));
        }

        return snapshots;
    }

    private sealed record SubValuation(decimal Value, decimal Cash, decimal Cost);

    private sealed class DayFlows(List<Guid> subs)
    {
        public Dictionary<Guid, decimal> OfSub { get; } = subs.ToDictionary(sub => sub, _ => 0m);
        public decimal OfAccount { get; private set; }

        public void AddTransfer(Guid sub, decimal amount) => OfSub[sub] += amount;

        public void AddExternal(Guid sub, decimal amount)
        {
            OfSub[sub] += amount;
            OfAccount += amount;
        }
    }

    private sealed class AccountTotals
    {
        public decimal Value { get; private set; }
        public decimal Cash { get; private set; }
        public decimal Cost { get; private set; }

        public void Add(decimal value, decimal cash, decimal cost)
        {
            Value += value;
            Cash += cash;
            Cost += cost;
        }
    }

    private sealed class PositionBook(List<Guid> subs, PriceOn priceOn, FxOn? fxOn)
    {
        private readonly Dictionary<(Guid Sub, Guid Instrument), decimal> _quantities = [];
        private readonly Dictionary<Guid, decimal> _cash = subs.ToDictionary(sub => sub, _ => 0m);
        private readonly Dictionary<Guid, decimal> _lastTradePrice = [];
        private readonly Dictionary<(Guid Sub, string Currency), decimal> _foreign = [];
        private readonly Dictionary<string, decimal> _lastRate = [];
        private readonly CostBook _costs = new();

        public void Apply(Movement movement, DateOnly day, DayFlows flows)
        {
            _costs.Apply(movement);

            if (movement.InstrumentId is { } instrument)
                ApplyToPosition(movement, instrument, day, flows);

            ApplyToCash(movement);
            flows.AddExternal(movement.SubPortfolioId, movement.ExternalFlow);
        }

        public Result<SubValuation> ValuationOf(Guid sub, DateOnly day)
        {
            var foreignInAccountCurrency = ForeignCashOf(sub, day);
            if (foreignInAccountCurrency.IsFailure)
                return foreignInAccountCurrency.Error;

            var cash = _cash[sub] + foreignInAccountCurrency.Value;
            var positions = _quantities
                .Where(entry => entry.Key.Sub == sub && entry.Value != 0m)
                .Sum(entry => entry.Value * Price(entry.Key.Instrument, day));

            return new SubValuation(cash + positions, cash, _costs.Of(sub));
        }

        private Result<decimal> ForeignCashOf(Guid sub, DateOnly day)
        {
            var total = 0m;

            foreach (var (currency, balance) in HeldForeignCash(sub))
            {
                if (Rate(currency, day) is not { } rate)
                    return ValuationErrors.NoExchangeRate(currency, day);

                total += balance * rate;
            }

            return total;
        }

        private IEnumerable<(string Currency, decimal Balance)> HeldForeignCash(Guid sub) =>
            _foreign.Where(entry => entry.Key.Sub == sub && entry.Value != 0m).Select(entry => (entry.Key.Currency, entry.Value));

        private void ApplyToPosition(Movement movement, Guid instrument, DateOnly day, DayFlows flows)
        {
            var key = (movement.SubPortfolioId, instrument);
            _quantities[key] = _quantities.GetValueOrDefault(key) + movement.QuantityDelta;

            if (movement.TradePrice is { } tradePrice)
                _lastTradePrice[instrument] = tradePrice;

            if (movement.TransferQuantity != 0m)
                flows.AddTransfer(movement.SubPortfolioId, movement.TransferQuantity * Price(instrument, day));
        }

        private void ApplyToCash(Movement movement)
        {
            if (movement.CashCurrency is { } currency)
            {
                var key = (movement.SubPortfolioId, currency);
                _foreign[key] = _foreign.GetValueOrDefault(key) + movement.CashDelta;
            }
            else
                _cash[movement.SubPortfolioId] += movement.CashDelta;
        }

        private decimal? Rate(string currency, DateOnly day)
        {
            if (fxOn?.Invoke(currency, day) is { } rate)
                _lastRate[currency] = rate;

            return _lastRate.TryGetValue(currency, out var known) ? known : null;
        }

        private decimal Price(Guid instrument, DateOnly day) => priceOn(instrument, day) ?? _lastTradePrice.GetValueOrDefault(instrument);
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

            return new Snapshot(
                day,
                sub,
                decimal.Round(value, 2),
                decimal.Round(cash, 2),
                decimal.Round(flow, 2),
                decimal.Round(_contributions, 2),
                decimal.Round(_index, 8),
                decimal.Round(costBasis, 2));
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

        public decimal Of(Guid sub)
        {
            var ofSub = _lots.Where(entry => entry.Key.Sub == sub);

            return ofSub.Sum(entry => entry.Value.Sum(lot => lot.Quantity * lot.UnitCost));
        }

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
