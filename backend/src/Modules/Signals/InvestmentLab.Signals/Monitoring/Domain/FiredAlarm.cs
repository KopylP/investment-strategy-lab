namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record FiredAlarm(SignalOccurrence Occurrence, SubjectReading Reading);
