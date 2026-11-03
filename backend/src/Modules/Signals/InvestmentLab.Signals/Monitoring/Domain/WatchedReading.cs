namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record WatchedReading(string SubjectKey, decimal Value, DateOnly? HeldSince = null, decimal? UnitCost = null);
