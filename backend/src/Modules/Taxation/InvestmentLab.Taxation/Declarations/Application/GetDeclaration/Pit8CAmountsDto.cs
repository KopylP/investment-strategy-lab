using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record Pit8CAmountsDto(
    decimal Revenue,
    decimal Costs)
{
    public static Pit8CAmountsDto From(Pit8CAmounts pit8CAmounts) => new(
        Revenue: pit8CAmounts.Revenue,
        Costs: pit8CAmounts.Costs);
}
