namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record LossCarryforwardDto(Guid Id, int Year, decimal Amount, decimal AlreadyUsed);
