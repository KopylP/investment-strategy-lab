using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed record ReplayWindow(Guid StrategyId, int Version, string? ParametersOverride, DateOnly? Start, DateOnly? End)
{
    public static ReplayWindow Of(ReplaySetup setup) => new(
        StrategyId: setup.StrategyId,
        Version: setup.Version,
        ParametersOverride: setup.ParametersOverride,
        Start: setup.Experiment?.Start,
        End: setup.Experiment?.End);
}
