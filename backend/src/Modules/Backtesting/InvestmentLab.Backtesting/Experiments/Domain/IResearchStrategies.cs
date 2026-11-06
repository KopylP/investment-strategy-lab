namespace InvestmentLab.Backtesting.Experiments.Domain;

public interface IResearchStrategies
{
    Task<ResearchSource?> FindAsync(Guid strategyId, int version, CancellationToken cancellationToken);

    Task<Result<int>> CheckAsync(string typeCode, string parameters, CancellationToken cancellationToken);
}
