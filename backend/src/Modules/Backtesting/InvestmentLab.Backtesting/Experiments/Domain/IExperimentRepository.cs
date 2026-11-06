namespace InvestmentLab.Backtesting.Experiments.Domain;

public interface IExperimentRepository
{
    Task<ResearchExperiment?> FindAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid?> NextAsync(CancellationToken cancellationToken);
    Task RemoveOfStrategyAsync(Guid strategyId, CancellationToken cancellationToken);
    void Add(ResearchExperiment experiment);
    Task SaveAsync(CancellationToken cancellationToken);
}
