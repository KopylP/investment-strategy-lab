using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public interface IStrategyReader
{
    Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken);
}
