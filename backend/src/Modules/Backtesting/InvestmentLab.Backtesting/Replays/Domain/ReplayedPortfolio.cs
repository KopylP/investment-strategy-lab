namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayedPortfolio(ReplaySetup setup, PriceTable prices, ReplayBook book, ReplayAlarms alarms)
{
    private const decimal DefaultCapital = 100_000m;

    private readonly List<GrowthDay> _growth = [];
    private readonly ReplayOrders _orders = new();
    private readonly ReplayJournal _journal = new();
    private DateOnly? _previousSession;
    private int _riskExits;
    private int _signalReports;

    public ReplaySetup Setup => setup;
    public PriceTable Prices => prices;
    public ReplayBook Book => book;
    public IReadOnlyList<GrowthDay> Growth => _growth;
    public decimal Capital => setup.Experiment?.InitialCapital ?? DefaultCapital;
    public bool HasPositions => book.Positions.Count > 0;
    private bool RecordsJournal => setup.Experiment?.RecordJournal == true;

    public static ReplayedPortfolio Start(ReplaySetup setup, PriceTable prices, ReplaySignalWatches watches)
    {
        var capital = setup.Experiment?.InitialCapital ?? DefaultCapital;
        var book = new ReplayBook(capital, setup.Experiment?.Costs);
        var alarms = new ReplayAlarms(setup.Signals, setup.LogicSignalIds ?? [], watches);

        return new ReplayedPortfolio(setup, prices, book, alarms);
    }

    public IReadOnlyList<BookTrade> FinalLiquidation()
    {
        var last = prices.Days.Count - 1;

        return book.LiquidationOn(prices.Days[last], prices.PricesOn(last));
    }

    public ReplaySession? Advance(int dayIndex)
    {
        var day = prices.Days[dayIndex];
        var marks = prices.PricesOn(dayIndex);
        var tradable = prices.TradablePricesOn(dayIndex);

        ExecutePending(tradable, day);
        RecordValue(day, marks);

        if (!setup.Universe.Any(tradable.ContainsKey))
            return null;

        var session = SessionOf(day, marks);
        _previousSession = day;
        CountSignalReports(session);

        return session;
    }

    public void Rebalance(ReplaySession session, ReplayDecision decision, IReadOnlyList<ReplayTrade> trades)
    {
        _orders.Rebalance(trades, session.Date, decision.Explanation);

        var targets = BudgetedTargets(decision);
        var reason = session.RebalanceBySignal ? "research.journal.signal" : "research.journal.schedule";
        RecordDecision(session, "rebalance", reason, targets, decision.Explanation);
    }

    public void ReviewRisk(ReplaySession session, ReplayRiskDecision assessment)
    {
        _orders.ExitPositions(book.Positions, assessment, session.Date);

        var reason = session.RiskReviewBySignal ? "research.journal.signal" : "research.journal.schedule";
        RecordDecision(session, "risk", reason, [], assessment.Explanation);
    }

    public ReplayResult ResultFrom(IReadOnlyList<ReplaySeries> series) => new(
        Start: _growth[0].Date,
        End: _growth[^1].Date,
        Currency: setup.Currency,
        Trades: book.Trades,
        RiskExits: _riskExits,
        SignalReports: _signalReports,
        Series: series,
        Journal: RecordsJournal ? _journal.Entries : null,
        InitialCapital: Capital);

    private ReplaySession SessionOf(DateOnly day, IReadOnlyDictionary<Guid, decimal> marks)
    {
        var moment = new ReplayMoment(day, book, marks, _growth[^1].Growth);
        var signals = alarms.FiredOn(moment);

        return new ReplaySession(
            Date: day,
            Prices: marks,
            Signals: signals,
            RebalanceScheduled: RebalanceDue(day),
            RiskReviewScheduled: RiskReviewDue(day));
    }

    private bool RebalanceDue(DateOnly day) => setup.RebalanceSchedule is { } schedule
        ? ReplayCalendar.IsDue(schedule, _previousSession, day)
        : ReplayCalendar.OpensPeriod(setup.RebalanceCadence, _previousSession, day);

    private bool RiskReviewDue(DateOnly day)
    {
        if (setup.RiskReviewSchedule is { } schedule)
            return ReplayCalendar.IsDue(schedule, _previousSession, day);

        return setup.RiskReviewCadence is { } cadence && ReplayCalendar.OpensPeriod(cadence, _previousSession, day);
    }

    private void CountSignalReports(ReplaySession session)
    {
        if (session.RebalanceBySignal)
            _signalReports++;

        if (session.RiskReviewBySignal)
            _signalReports++;
    }

    private void ExecutePending(IReadOnlyDictionary<Guid, decimal> quotes, DateOnly day)
    {
        var exitsBefore = _riskExits;
        var orders = _orders.TakeForExecution();

        foreach (var order in orders)
            Execute(order, quotes, day);

        if (_riskExits > exitsBefore)
            ParkRiskProceeds(quotes, day);
    }

    private void Execute(PendingReplayOrder order, IReadOnlyDictionary<Guid, decimal> quotes, DateOnly day)
    {
        if (order.HasExpired(day))
        {
            RecordExpiry(day);
            return;
        }

        if (!quotes.TryGetValue(order.Trade.InstrumentId, out var quote))
        {
            _orders.Keep(order);
            return;
        }

        var filled = book.Execute(order.Trade, quote, day);
        if (filled is null)
            return;

        if (order.IsRiskExit)
            _riskExits++;

        RecordFill(filled, order.PlacedOn, order.Reason);
    }

    private void ParkRiskProceeds(IReadOnlyDictionary<Guid, decimal> quotes, DateOnly day)
    {
        if (setup.CashInstrumentId is not { } cash || !quotes.TryGetValue(cash, out var quote))
            return;

        var trade = new ReplayTrade(cash, IsSale: false, Quantity: 0m, Amount: book.Cash);
        var filled = book.Execute(trade, quote, day);
        if (filled is not null)
            RecordFill(filled, day, Message.Of("research.journal.risk_cash"));
    }

    private void RecordValue(DateOnly day, IReadOnlyDictionary<Guid, decimal> marks)
    {
        var growth = (double)(book.ValueAt(marks) / Capital);
        _growth.Add(new GrowthDay(day, growth));
    }

    private void RecordDecision(
        ReplaySession session,
        string kind,
        string reason,
        IReadOnlyList<ReplayTarget> targets,
        ReplayExplanation? explanation)
    {
        if (!RecordsJournal)
            return;

        var allocation = ReplayAllocation.Of(book, session.Prices, targets);
        var entry = new ReplayJournalEntry(
            Date: session.Date,
            Kind: kind,
            Reason: Message.Of(reason),
            Allocation: allocation,
            Explanation: explanation);

        _journal.Add(entry);
    }

    private void RecordFill(BookTrade trade, DateOnly decisionDate, string reason)
    {
        if (!RecordsJournal)
            return;

        var fill = new ReplayFill(
            InstrumentId: trade.InstrumentId,
            Quantity: trade.Quantity,
            Price: trade.Price,
            ReferencePrice: trade.ReferencePrice ?? trade.Price,
            Commission: trade.Fee,
            SpreadCost: trade.SpreadCost,
            SlippageCost: trade.SlippageCost,
            DecisionDate: decisionDate);
        var entry = new ReplayJournalEntry(trade.On, trade.IsSale ? "sell" : "buy", reason, Fill: fill);

        _journal.Add(entry);
    }

    private void RecordExpiry(DateOnly day)
    {
        if (!RecordsJournal)
            return;

        var entry = new ReplayJournalEntry(day, "expired", Message.Of("research.journal.expired"));
        _journal.Add(entry);
    }

    private static IReadOnlyList<ReplayTarget> BudgetedTargets(ReplayDecision decision)
    {
        return [.. decision.Targets.Select(target => target with { Weight = target.Weight * decision.InvestFraction })];
    }
}
