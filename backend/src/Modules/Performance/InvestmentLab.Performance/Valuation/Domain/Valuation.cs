namespace InvestmentLab.Performance.Valuation.Domain;

/// <summary>
/// One change to a sub-portfolio, derived from a journal entry. <see cref="ExternalFlow"/> is money from or to the
/// outside (contributions +, withdrawals −); a transfer between sub-portfolios is a flow for each side, valued that day.
/// </summary>
/// <param name="TransferQuantity">Units moved by an internal transfer (+ in, − out); valued at the day's price as a flow.</param>
/// <param name="CashCurrency">
/// Null: <see cref="CashDelta"/> is in the account currency. Otherwise it moves the balance the broker keeps in that
/// currency, which is valued every day at that day's rate (so its value follows the rate, as at the broker).
/// </param>
public sealed record Movement(
    DateOnly Date,
    int Sequence,
    Guid SubPortfolioId,
    Guid? InstrumentId,
    decimal QuantityDelta,
    decimal CashDelta,
    decimal ExternalFlow,
    decimal TransferQuantity = 0m,
    decimal? TradePrice = null,
    string? CashCurrency = null);

/// <summary>Value of the account (no sub-portfolio) or of one sub-portfolio at the end of a day.</summary>
/// <param name="Flow">External money that came in (or out) that day, counted at the start of the day.</param>
/// <param name="TwrIndex">Time-weighted growth of 1 since the first day.</param>
public sealed record Snapshot(
    DateOnly Date,
    Guid? SubPortfolioId,
    decimal Value,
    decimal Cash,
    decimal Flow,
    decimal NetContributions,
    decimal TwrIndex);

/// <summary>The latest known price of an instrument on or before a day, in the account currency.</summary>
public delegate decimal? PriceOn(Guid instrumentId, DateOnly day);

/// <summary>Account-currency units for one unit of <paramref name="currency"/> on or before a day.</summary>
public delegate decimal? FxOn(string currency, DateOnly day);

/// <summary>
/// Replays movements day by day (§6.8 ValuationCalculator + TwrCalculator): quantities and cash per sub-portfolio,
/// valued at the latest price; TWR chains daily returns with flows at the start of the day.
/// </summary>
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
            foreach (var sub in subs)
            {
                var subCash = cash[sub] + foreign.Where(entry => entry.Key.Sub == sub && entry.Value != 0m)
                    .Sum(entry => entry.Value * Rate(entry.Key.Currency, day));
                var value = subCash + quantities.Where(entry => entry.Key.Sub == sub && entry.Value != 0m)
                    .Sum(entry => entry.Value * Price(entry.Key.Instrument, day));
                accountValue += value;
                accountCash += subCash;
                snapshots.Add(state[sub].Next(day, sub, value, subCash, flows[sub]));
            }
            snapshots.Add(state[Guid.Empty].Next(day, null, accountValue, accountCash, accountFlow));
        }
        return snapshots;
    }

    private sealed class Chain
    {
        private decimal _previousValue;
        private decimal _index = 1m;
        private decimal _contributions;

        public Snapshot Next(DateOnly day, Guid? sub, decimal value, decimal cash, decimal flow)
        {
            var start = _previousValue + flow;
            if (start > 0m)
                _index *= value / start;
            _contributions += flow;
            _previousValue = value;
            return new Snapshot(day, sub, decimal.Round(value, 2), decimal.Round(cash, 2), decimal.Round(flow, 2), decimal.Round(_contributions, 2), decimal.Round(_index, 8));
        }
    }
}

/// <summary>Peak-to-trough fall of the TWR index (§6.8 DrawdownCalculator).</summary>
public static class DrawdownCalculator
{
    public sealed record Drawdown(decimal Max, decimal Current, DateOnly? PeakDate, DateOnly? TroughDate);

    public static Drawdown Of(IReadOnlyList<(DateOnly Date, decimal Index)> series)
    {
        decimal peak = 0m, max = 0m, current = 0m;
        DateOnly? peakDate = null, troughDate = null, maxPeakDate = null;
        foreach (var (date, index) in series)
        {
            if (index > peak)
                (peak, peakDate) = (index, date);
            current = peak > 0m ? index / peak - 1m : 0m;
            if (current < max)
                (max, troughDate, maxPeakDate) = (current, date, peakDate);
        }
        return new Drawdown(decimal.Round(max, 6), decimal.Round(current, 6), maxPeakDate, troughDate);
    }
}

/// <summary>Money-weighted annual return (§6.8 XirrCalculator): the rate at which contributions grow into today's value.</summary>
public static class XirrCalculator
{
    /// <param name="flows">From the investor's side: contributions negative, withdrawals and the final value positive.</param>
    public static decimal? Of(IReadOnlyList<(DateOnly Date, decimal Amount)> flows)
    {
        if (flows.Count < 2 || !flows.Any(flow => flow.Amount < 0m) || !flows.Any(flow => flow.Amount > 0m))
            return null;

        var start = flows.Min(flow => flow.Date);
        var points = flows.Select(flow => (Years: (flow.Date.DayNumber - start.DayNumber) / 365.0, Amount: (double)flow.Amount)).ToList();
        if (points.Max(point => point.Years) < 1.0 / 365)
            return null;

        double Npv(double rate) => points.Sum(point => point.Amount / Math.Pow(1 + rate, point.Years));

        // Bisection on (-99%, +1000%): robust where Newton diverges, plenty precise for a dashboard.
        double low = -0.99, high = 10.0;
        if (Npv(low) * Npv(high) > 0)
            return null;
        for (var i = 0; i < 200; i++)
        {
            var mid = (low + high) / 2;
            if (Npv(low) * Npv(mid) <= 0)
                high = mid;
            else
                low = mid;
        }
        return decimal.Round((decimal)((low + high) / 2), 6);
    }
}
