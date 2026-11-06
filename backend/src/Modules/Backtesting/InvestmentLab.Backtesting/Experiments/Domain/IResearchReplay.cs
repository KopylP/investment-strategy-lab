using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public interface IResearchReplay
{
    Task<Result<ReplayResult>> RunAsync(ReplaySetup setup, CancellationToken cancellationToken);
}
