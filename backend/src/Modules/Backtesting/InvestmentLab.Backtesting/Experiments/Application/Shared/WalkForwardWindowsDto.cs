using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record WalkForwardWindowsDto(
    int TrainingMonths,
    int TestMonths)
{
    public static WalkForwardWindowsDto From(WalkForwardWindows walkForwardWindows) => new(
        TrainingMonths: walkForwardWindows.TrainingMonths,
        TestMonths: walkForwardWindows.TestMonths);

    public WalkForwardWindows ToDomain() => new(
        TrainingMonths: TrainingMonths,
        TestMonths: TestMonths);
}
