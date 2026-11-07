namespace InvestmentLab.Backtesting.Replays.Domain;

public static class ReplayReadiness
{
    private const int MaximumBoundaryGapDays = 7;

    public static Error? ScheduleProblem(ReplaySetup setup) =>
        setup.UnsupportedSchedule is { } unsupported ? BacktestErrors.UnsupportedSchedule(unsupported) : null;

    public static Result<int> FirstDay(ReplaySetup setup, PriceTable prices)
    {
        var firstDay = prices.FirstDayToReplay(setup);

        if (setup.Experiment is { } requested && MissesStart(requested, prices, firstDay))
            return ShortfallError(setup, prices, requested);

        if (firstDay is null)
            return BacktestErrors.NotEnoughHistory();

        if (setup.Experiment is { } experiment && MissesEnd(experiment, prices))
            return BacktestErrors.HistoryEnds(prices.Days[^1], experiment.End);

        return firstDay.Value;
    }

    private static bool MissesStart(ReplayExperiment requested, PriceTable prices, int? firstDay)
    {
        if (firstDay is null)
            return true;

        var missingAtStart = prices.Days[firstDay.Value].DayNumber - requested.Start.DayNumber;

        return missingAtStart > MaximumBoundaryGapDays;
    }

    private static bool MissesEnd(ReplayExperiment requested, PriceTable prices) =>
        requested.End.DayNumber - prices.Days[^1].DayNumber > MaximumBoundaryGapDays;

    private static Error ShortfallError(ReplaySetup setup, PriceTable prices, ReplayExperiment requested)
    {
        var shortfall = prices.ShortfallAt(setup, requested.Start);
        if (shortfall is null)
            return BacktestErrors.NotEnoughHistory();

        var limit = shortfall.Limit.Describe(id => LabelOf(setup, id));

        return BacktestErrors.MissingExperimentHistory(limit, shortfall.EarliestStart);
    }

    private static string LabelOf(ReplaySetup setup, Guid instrumentId)
    {
        if (instrumentId == setup.Benchmark.InstrumentId)
            return setup.Benchmark.Label;

        return setup.InstrumentLabels?.GetValueOrDefault(instrumentId) ?? instrumentId.ToString();
    }
}
