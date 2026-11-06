namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record BacktestRun(Guid StrategyId, int Version, Guid? ExperimentId = null);
