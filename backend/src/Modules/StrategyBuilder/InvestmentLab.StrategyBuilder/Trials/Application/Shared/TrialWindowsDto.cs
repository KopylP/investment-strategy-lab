using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed record TrialWindowsDto(int TrainingMonths, int TestMonths)
{
    public static TrialWindowsDto From(TrialWindows windows) => new(windows.TrainingMonths, windows.TestMonths);

    public TrialWindows ToDomain() => new(TrainingMonths, TestMonths);
}
