using System.ComponentModel.DataAnnotations;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

public sealed class ReplayRunOptions
{
    public const string Section = "Backtesting:Runs";

    private const int OneBacktestAndOneExperiment = 2;

    [Range(1, OneBacktestAndOneExperiment)]
    public int MaxConcurrentRuns { get; init; } = 1;

    [Range(1, int.MaxValue)]
    public int MemoryWarningMegabytes { get; init; } = 1024;
}
