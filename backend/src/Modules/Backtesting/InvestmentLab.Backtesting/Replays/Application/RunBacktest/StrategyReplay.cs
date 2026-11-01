using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.RunBacktest;

public sealed class StrategyReplay(IPriceTables priceTables, IStrategyAnswers answers, ITargetTrades targetTrades)
{
    private const decimal NotionalCapital = 100_000m;

    public async Task<Result<ReplayResult>> RunAsync(ReplaySetup setup, CancellationToken cancellationToken)
    {
        var prices = await priceTables.LoadAsync(setup, cancellationToken);

        if (prices.FirstDayToReplay(setup) is not { } firstDay)
            return BacktestErrors.NotEnoughHistory();

        var run = new Run(setup, prices, new ReplayBook(NotionalCapital));

        for (var dayIndex = firstDay; dayIndex < prices.Days.Count; dayIndex++)
        {
            var stepped = await StepAsync(run, dayIndex, firstDay, cancellationToken);
            if (stepped.IsFailure)
                return stepped.Error;
        }

        return run.ResultFrom(firstDay);
    }

    private async Task<Result> StepAsync(Run run, int dayIndex, int firstDay, CancellationToken cancellationToken)
    {
        var day = run.Prices.Days[dayIndex];
        var today = run.Prices.PricesOn(dayIndex);
        var previous = dayIndex == firstDay ? (DateOnly?)null : run.Prices.Days[dayIndex - 1];

        run.ExecutePending(today, day);
        run.Value(day, today);

        if (ReplayCalendar.OpensPeriod(run.Setup.RebalanceCadence, previous, day))
            return await RebalanceAsync(run, day, today, cancellationToken);

        if (run.Setup.RiskReviewCadence is { } review && ReplayCalendar.OpensPeriod(review, previous, day))
            return await ReviewRiskAsync(run, day, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> RebalanceAsync(Run run, DateOnly day, IReadOnlyDictionary<Guid, decimal> today, CancellationToken cancellationToken)
    {
        var decision = await answers.DecideAsync(run.Setup, run.Book, day, cancellationToken);
        if (decision.IsFailure)
            return decision.Error;

        run.Order(targetTrades.TradesFor(decision.Value, run.Setup, run.Book, today));

        return Result.Success();
    }

    private async Task<Result> ReviewRiskAsync(Run run, DateOnly day, CancellationToken cancellationToken)
    {
        if (run.Book.Positions.Count == 0)
            return Result.Success();

        var exits = await answers.ExitsAsync(run.Setup, run.Book, day, cancellationToken);
        if (exits.IsFailure)
            return exits.Error;

        run.OrderExits(exits.Value);

        return Result.Success();
    }

    private sealed class Run(ReplaySetup setup, PriceTable prices, ReplayBook book)
    {
        private readonly List<GrowthDay> _growth = [];
        private List<ReplayTrade> _pending = [];
        private bool _parkAfterwards;
        private int _riskExits;

        public ReplaySetup Setup => setup;
        public PriceTable Prices => prices;
        public ReplayBook Book => book;

        public void Order(IReadOnlyList<ReplayTrade> trades) => _pending = [.. trades];

        public void OrderExits(IReadOnlyList<Guid> instruments)
        {
            var held = book.Positions.Where(position => instruments.Contains(position.InstrumentId)).ToList();

            _pending = [.. held.Select(position => new ReplayTrade(position.InstrumentId, true, position.Quantity, 0m))];
            _parkAfterwards = held.Count > 0;
            _riskExits += held.Count;
        }

        public void ExecutePending(IReadOnlyDictionary<Guid, decimal> today, DateOnly day)
        {
            foreach (var trade in _pending.Where(trade => today.ContainsKey(trade.InstrumentId)))
            {
                if (trade.IsSale)
                    book.Sell(trade.InstrumentId, trade.Quantity, today[trade.InstrumentId]);
                else
                    book.Buy(trade.InstrumentId, trade.Amount, today[trade.InstrumentId], day);
            }

            if (_parkAfterwards && setup.CashInstrumentId is { } cash && today.TryGetValue(cash, out var price))
                book.Buy(cash, book.Cash, price, day);

            _pending = [];
            _parkAfterwards = false;
        }

        public void Value(DateOnly day, IReadOnlyDictionary<Guid, decimal> today) =>
            _growth.Add(new GrowthDay(day, (double)(book.ValueAt(today) / NotionalCapital)));

        public ReplayResult ResultFrom(int firstDay)
        {
            var comparisons = new ReplayComparisons(prices, firstDay, setup);
            var riskFree = comparisons.RiskFreeRate;

            return new ReplayResult(
                Start: _growth[0].Date,
                End: _growth[^1].Date,
                Trades: book.Trades,
                RiskExits: _riskExits,
                Series:
                [
                    ReplaySeries.Of(ReplaySeriesKind.Strategy, _growth, riskFree),
                    ReplaySeries.Of(ReplaySeriesKind.Benchmark, comparisons.Benchmark, riskFree),
                    ReplaySeries.Of(ReplaySeriesKind.EqualWeight, comparisons.EqualWeight(), riskFree),
                ]);
        }
    }
}
