namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SubjectReading(string Key, string Label, decimal Value, DateOnly DataDate);
