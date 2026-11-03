using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Domain.Comparisons;

namespace InvestmentLab.Backtesting.Replays.Application.RunBacktest;

public sealed class StrategyReplay(
    IPriceTables priceTables,
    IStrategyAnswers answers,
    ITargetTrades targetTrades,
    IReplaySignals signals,
    IReplayTaxes taxes,
    ReplayComparisonCatalog comparisonCatalog)
{
    private const decimal NotionalCapital = 100_000m;

    public async Task<Result<ReplayResult>> RunAsync(ReplaySetup setup, CancellationToken cancellationToken)
    {
        var comparisons = comparisonCatalog.Resolve(setup.Comparisons);
        if (comparisons.IsFailure)
            return comparisons.Error;

        var watches = await WatchesAsync(setup, cancellationToken);
        if (watches.IsFailure)
            return watches.Error;

        var signalInstruments = watches.Value.User.Values.SelectMany(watch => watch.Instruments).Distinct().ToList();
        setup = setup with { SignalInstruments = signalInstruments };

        var prices = await priceTables.LoadAsync(setup, cancellationToken);

        if (prices.FirstDayToReplay(setup) is not { } firstDay)
            return BacktestErrors.NotEnoughHistory();

        var alarms = new ReplayAlarms(setup.Signals, setup.LogicSignalIds ?? [], watches.Value);
        var run = new Run(setup, prices, new ReplayBook(NotionalCapital), alarms);

        for (var dayIndex = firstDay; dayIndex < prices.Days.Count; dayIndex++)
        {
            var stepped = await StepAsync(run, dayIndex, firstDay, cancellationToken);
            if (stepped.IsFailure)
                return stepped.Error;
        }

        var history = new ReplayedHistory(prices, firstDay, setup, run.Growth);
        var series = await SeriesOfAsync(run, history, comparisons.Value, cancellationToken);

        return run.ResultFrom(series);
    }

    private async Task<Result<ReplaySignalWatches>> WatchesAsync(ReplaySetup setup, CancellationToken cancellationToken)
    {
        var builtIn = setup.Signals.AllKinds.ToDictionary(kind => kind, signals.Watch);
        var user = new Dictionary<Guid, IReplaySignalWatch>();
        var logicIds = setup.LogicSignalIds ?? [];
        var ids = logicIds.Concat(setup.Signals.AllSignalIds).Distinct();

        foreach (var id in ids)
        {
            var watch = await signals.WatchSignalAsync(id, cancellationToken);
            if (watch is null && logicIds.Contains(id))
                return BacktestErrors.SignalMissing(id);

            if (watch is not null)
                user.Add(id, watch);
        }

        return new ReplaySignalWatches(builtIn, user);
    }

    private async Task<List<ReplaySeries>> SeriesOfAsync(
        Run run, ReplayedHistory history, IReadOnlyList<IReplayComparison> comparisons, CancellationToken cancellationToken)
    {
        var riskFree = history.RiskFreeRate;
        var series = new List<ReplaySeries> { await StrategySeriesAsync(run, riskFree, cancellationToken) };

        foreach (var comparison in comparisons)
        {
            var line = comparison.LineOf(history);
            series.Add(await ComparedSeriesAsync(line, run.Setup.Currency, riskFree, cancellationToken));
        }

        return series;
    }

    private async Task<ReplaySeries> StrategySeriesAsync(Run run, double riskFree, CancellationToken cancellationToken)
    {
        var lastDay = run.Prices.Days.Count - 1;
        var liquidation = run.Book.LiquidationOn(run.Prices.Days[lastDay], run.Prices.PricesOn(lastDay));

        var yearly = await taxes.YearlyAsync([.. run.Book.TradeLog, .. liquidation], run.Setup.Currency, cancellationToken);
        var afterTax = AfterTaxGrowth.FinalOf(run.Growth, yearly, NotionalCapital);

        return ReplaySeries.Of(ReplaySeries.StrategyCode, Message.Of("backtest.series.strategy"), run.Growth, riskFree, afterTax);
    }

    private async Task<ReplaySeries> ComparedSeriesAsync(ComparedLine line, string currency, double riskFree, CancellationToken cancellationToken)
    {
        var trades = BookTrade.HeldThroughout(line.HeldAs, line.Growth, NotionalCapital);

        var yearly = await taxes.YearlyAsync(trades, currency, cancellationToken);
        var afterTax = AfterTaxGrowth.FinalOf(line.Growth, yearly, NotionalCapital);

        return ReplaySeries.Of(line.Code, line.Label, line.Growth, riskFree, afterTax);
    }

    private async Task<Result> StepAsync(Run run, int dayIndex, int firstDay, CancellationToken cancellationToken)
    {
        var day = run.Prices.Days[dayIndex];
        var today = run.Prices.PricesOn(dayIndex);
        var previous = dayIndex == firstDay ? (DateOnly?)null : run.Prices.Days[dayIndex - 1];

        run.ExecutePending(today, day);
        run.Value(day, today);
        var fired = run.AlarmsOn(day, today);

        var rebalanceDue = ReplayCalendar.OpensPeriod(run.Setup.RebalanceCadence, previous, day);
        if (rebalanceDue || fired.Rebalance)
            return await RebalanceAsync(run, day, today, broughtBySignal: !rebalanceDue, fired.States ?? [], cancellationToken);

        var riskReviewDue = IsRiskReviewDue(run.Setup, previous, day);
        if (riskReviewDue || fired.RiskReview)
            return await ReviewRiskAsync(run, day, broughtBySignal: !riskReviewDue, fired.States ?? [], cancellationToken);

        return Result.Success();
    }

    private static bool IsRiskReviewDue(ReplaySetup setup, DateOnly? previous, DateOnly day) =>
        setup.RiskReviewCadence is { } review && ReplayCalendar.OpensPeriod(review, previous, day);

    private async Task<Result> RebalanceAsync(
        Run run, DateOnly day, IReadOnlyDictionary<Guid, decimal> today, bool broughtBySignal, IReadOnlyList<ReplaySignalState> states, CancellationToken cancellationToken)
    {
        run.NoteReport(broughtBySignal);

        var decision = await answers.DecideAsync(run.Setup, run.Book, day, states, cancellationToken);
        if (decision.IsFailure)
            return decision.Error;

        run.Order(targetTrades.TradesFor(decision.Value, run.Setup, run.Book, today));

        return Result.Success();
    }

    private async Task<Result> ReviewRiskAsync(Run run, DateOnly day, bool broughtBySignal, IReadOnlyList<ReplaySignalState> states, CancellationToken cancellationToken)
    {
        run.NoteReport(broughtBySignal);

        if (run.Book.Positions.Count == 0)
            return Result.Success();

        var exits = await answers.ExitsAsync(run.Setup, run.Book, day, states, cancellationToken);
        if (exits.IsFailure)
            return exits.Error;

        run.OrderExits(exits.Value);

        return Result.Success();
    }

    private sealed class Run(ReplaySetup setup, PriceTable prices, ReplayBook book, ReplayAlarms alarms)
    {
        private readonly List<GrowthDay> _growth = [];
        private List<ReplayTrade> _pending = [];
        private bool _parkAfterwards;
        private int _riskExits;
        private int _signalReports;

        public ReplaySetup Setup => setup;
        public PriceTable Prices => prices;
        public ReplayBook Book => book;
        public IReadOnlyList<GrowthDay> Growth => _growth;

        public FiredReports AlarmsOn(DateOnly day, IReadOnlyDictionary<Guid, decimal> today) =>
            alarms.FiredOn(new ReplayMoment(day, book, today, _growth[^1].Growth));

        public void NoteReport(bool broughtBySignal)
        {
            if (broughtBySignal)
                _signalReports++;
        }

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
                    book.Sell(trade.InstrumentId, trade.Quantity, today[trade.InstrumentId], day);
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

        public ReplayResult ResultFrom(IReadOnlyList<ReplaySeries> series) =>
            new(
                Start: _growth[0].Date,
                End: _growth[^1].Date,
                Currency: setup.Currency,
                Trades: book.Trades,
                RiskExits: _riskExits,
                SignalReports: _signalReports,
                Series: series);
    }
}
