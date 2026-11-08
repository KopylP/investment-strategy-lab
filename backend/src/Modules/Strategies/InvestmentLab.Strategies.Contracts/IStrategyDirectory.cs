namespace InvestmentLab.Strategies.Contracts;

public interface IStrategyDirectory
{
    Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken);

    Task<StrategyReplaySetupDto?> GetReplaySetup(Guid strategyId, int? version, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> GetMessages(string languageCode, CancellationToken cancellationToken);
}
