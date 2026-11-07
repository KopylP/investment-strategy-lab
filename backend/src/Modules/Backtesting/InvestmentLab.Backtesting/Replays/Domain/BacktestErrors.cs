namespace InvestmentLab.Backtesting.Replays.Domain;

public static class BacktestErrors
{
    public static Error MissingExperimentHistory(string limit, DateOnly earliestStart) =>
        Error.Validation("research.history_missing", "The requested period has insufficient warmed-up price history.",
            new { limit, earliestStart = earliestStart.ToString("yyyy-MM-dd") });
    public static Error HistoryEnds(DateOnly lastQuote, DateOnly requestedEnd) =>
        Error.Validation("research.history_ends", "Quotes end before the requested period does.",
            new { lastQuote = lastQuote.ToString("yyyy-MM-dd"), end = requestedEnd.ToString("yyyy-MM-dd") });
    public static Error InvalidCosts() =>
        Error.Validation("research.costs_invalid", "Execution costs are outside the allowed range.");
    public static Error UnsupportedSchedule(string schedule) =>
        Error.Validation("backtest.unsupported_schedule", "Daily-close replay supports end-of-day reports at 20:00; an intraday schedule cannot be replayed faithfully.", new { schedule });
    public static Error SignalMissing(Guid id) => Error.Validation("backtest.signal_missing", "A signal referenced by the strategy no longer exists.", new { id });

    public static Error StrategyNotFound(Guid strategyId) =>
        Error.NotFound("backtest.strategy_not_found", $"Strategy {strategyId} or its version does not exist.");

    public static Error NotReady(Guid strategyId) =>
        Error.Conflict("backtest.not_ready", $"Strategy {strategyId} has no finished backtest yet.");

    public static Error NotEnoughHistory() =>
        Error.Validation("backtest.not_enough_history", "There is too little price history to replay this strategy.");

    public static Error UnknownComparison(string code) =>
        Error.Validation("backtest.unknown_comparison", $"The strategy type asks to be compared with '{code}', which the backtest does not know.");

    public static Error InvalidMoneyPlan() =>
        Error.Validation("backtest.invalid_money_plan", "Start and contribution cannot be negative, and the period must be at least one year.");
}
