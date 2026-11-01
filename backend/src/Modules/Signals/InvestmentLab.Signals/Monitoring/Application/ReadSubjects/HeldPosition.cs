namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

public sealed record HeldPosition(QuoteSubject Subject, decimal AverageCost, DateOnly FirstOpenedOn);
