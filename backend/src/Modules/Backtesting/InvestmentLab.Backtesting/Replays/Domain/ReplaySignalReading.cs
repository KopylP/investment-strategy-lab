namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySignalReading(string SubjectKey, decimal Value, DateOnly? HeldSince = null);
