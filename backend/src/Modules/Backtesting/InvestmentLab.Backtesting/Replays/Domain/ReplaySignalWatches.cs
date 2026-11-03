namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySignalWatches(
    IReadOnlyDictionary<string, IReplaySignalWatch> BuiltIn,
    IReadOnlyDictionary<Guid, IReplaySignalWatch> User);
