using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.CompareBacktests;

public sealed record BacktestRunDto(
    Guid StrategyId,
    int Version,
    Guid? ExperimentId = null)
{
    public BacktestRun ToDomain() => new(
        StrategyId: StrategyId,
        Version: Version,
        ExperimentId: ExperimentId);
}
