namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySignalState(Guid SignalId, IReadOnlyList<Guid> ActiveFor);
