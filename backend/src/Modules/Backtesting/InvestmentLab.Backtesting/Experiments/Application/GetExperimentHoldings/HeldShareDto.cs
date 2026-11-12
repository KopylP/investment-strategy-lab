using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentHoldings;

public sealed record HeldShareDto(
    string Fund,
    double Weight)
{
    public static HeldShareDto From(HeldShare heldShare) => new(
        Fund: heldShare.Fund,
        Weight: heldShare.Weight);
}
