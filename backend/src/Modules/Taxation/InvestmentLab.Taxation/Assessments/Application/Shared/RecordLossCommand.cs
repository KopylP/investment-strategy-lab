namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record RecordLossCommand(int Year, decimal Amount, decimal AlreadyUsed = 0m);
