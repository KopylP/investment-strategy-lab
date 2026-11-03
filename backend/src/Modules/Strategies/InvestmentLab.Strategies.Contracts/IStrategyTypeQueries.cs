namespace InvestmentLab.Strategies.Contracts;

public interface IStrategyTypeQueries
{
    Task<StrategyTypeSchemaDto?> GetTypeAsync(string code, CancellationToken cancellationToken);
}
