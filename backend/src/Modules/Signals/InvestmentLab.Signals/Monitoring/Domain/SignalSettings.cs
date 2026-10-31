namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SignalSettings(string Name, SignalCondition Condition, Cadence Schedule, bool Enabled);
