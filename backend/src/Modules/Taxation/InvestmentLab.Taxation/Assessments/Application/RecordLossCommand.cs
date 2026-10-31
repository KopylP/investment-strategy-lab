namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record RecordLossCommand(int Year, decimal Amount, decimal AlreadyUsed = 0m);
