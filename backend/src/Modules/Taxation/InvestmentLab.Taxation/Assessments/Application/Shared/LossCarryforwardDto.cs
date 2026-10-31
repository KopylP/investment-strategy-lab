namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record LossCarryforwardDto(Guid Id, int Year, decimal Amount, decimal AlreadyUsed);
