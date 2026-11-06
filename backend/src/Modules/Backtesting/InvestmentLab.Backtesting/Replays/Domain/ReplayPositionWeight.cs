namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayPositionWeight(Guid InstrumentId, decimal Quantity, decimal Weight);
