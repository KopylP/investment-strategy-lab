namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed record FreshPrice(decimal Close, string Currency, DateOnly Date);
