namespace InvestmentLab.Backtesting.Replays.Domain;

public static class ReplayReadiness
{
    private const int MaximumBoundaryGapDays = 7;

    public static Error? ScheduleProblem(ReplaySetup setup) =>
        setup.UnsupportedSchedule is { } unsupported ? BacktestErrors.UnsupportedSchedule(unsupported) : null;

    public static Result<int> FirstDay(ReplaySetup setup, PriceTable prices)
    {
        var firstDay = prices.FirstDayToReplay(setup);
        if (firstDay is null)
            return BacktestErrors.NotEnoughHistory();

        if (HasMissingBoundary(setup, prices, firstDay.Value))
            return BacktestErrors.MissingExperimentHistory();

        return firstDay.Value;
    }

    private static bool HasMissingBoundary(ReplaySetup setup, PriceTable prices, int firstDay)
    {
        if (setup.Experiment is not { } requested)
            return false;

        var missingAtStart = prices.Days[firstDay].DayNumber - requested.Start.DayNumber;
        var missingAtEnd = requested.End.DayNumber - prices.Days[^1].DayNumber;

        return missingAtStart > MaximumBoundaryGapDays || missingAtEnd > MaximumBoundaryGapDays;
    }
}
