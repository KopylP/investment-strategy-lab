namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record BookLot(decimal Quantity, decimal UnitCost, DateOnly AcquiredOn);
