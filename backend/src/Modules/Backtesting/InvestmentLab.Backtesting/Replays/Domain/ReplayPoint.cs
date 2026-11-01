namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayPoint(DateOnly Date, double Growth, double WorstDrawdown);
