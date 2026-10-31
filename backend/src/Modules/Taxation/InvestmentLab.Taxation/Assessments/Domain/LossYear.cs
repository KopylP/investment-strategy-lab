namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record LossYear(int Year, decimal Amount, decimal AlreadyUsed = 0m);
