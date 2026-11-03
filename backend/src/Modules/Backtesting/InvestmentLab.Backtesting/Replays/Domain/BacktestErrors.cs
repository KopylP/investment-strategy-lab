namespace InvestmentLab.Backtesting.Replays.Domain;

public static class BacktestErrors
{
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
