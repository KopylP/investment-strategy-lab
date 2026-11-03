using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Domain;

public interface IStrategyRepository
{
    Task<Strategy?> FindAsync(StrategyId strategyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Strategy>> ListAsync(CancellationToken cancellationToken);

    Task<bool> NameTakenAsync(UserId ownerId, string name, CancellationToken cancellationToken);

    void Add(Strategy strategy);

    void Remove(Strategy strategy);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
