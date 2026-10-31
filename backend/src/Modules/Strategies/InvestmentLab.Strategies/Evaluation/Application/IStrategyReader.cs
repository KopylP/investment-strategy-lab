using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Evaluation.Application;

public interface IStrategyReader
{
    Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken);
}
