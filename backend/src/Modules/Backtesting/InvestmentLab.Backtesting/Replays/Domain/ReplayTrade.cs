namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayTrade(Guid InstrumentId, bool IsSale, decimal Quantity, decimal Amount);
