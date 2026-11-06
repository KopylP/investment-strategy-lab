namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySignalWatches(
    IReadOnlyDictionary<string, IReplaySignalWatch> BuiltIn,
    IReadOnlyDictionary<Guid, IReplaySignalWatch> User)
{
    public static Result<ReplaySignalWatches> Checked(
        IReadOnlyDictionary<string, IReplaySignalWatch> builtIn,
        IReadOnlyDictionary<Guid, IReplaySignalWatch> user,
        IReadOnlyList<Guid> requiredSignals)
    {
        foreach (var id in requiredSignals)
            if (!user.ContainsKey(id))
                return BacktestErrors.SignalMissing(id);

        return new ReplaySignalWatches(builtIn, user);
    }

    public static ReplaySetup WithInstruments(ReplaySetup setup, ReplaySignalWatches watches)
    {
        var instruments = watches.User.Values.SelectMany(watch => watch.Instruments).Distinct().ToList();

        return setup with { SignalInstruments = instruments };
    }
}
