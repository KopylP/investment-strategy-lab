using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record CarryforwardUseDto(
    int LossYear,
    decimal Amount)
{
    public static CarryforwardUseDto From(CarryforwardUse carryforwardUse) => new(
        LossYear: carryforwardUse.LossYear,
        Amount: carryforwardUse.Amount);
}
