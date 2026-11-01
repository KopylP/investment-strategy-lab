namespace InvestmentLab.Strategies.Contracts;

public interface IStrategyDirectory
{
    Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> GetMessages(CancellationToken cancellationToken);
}
