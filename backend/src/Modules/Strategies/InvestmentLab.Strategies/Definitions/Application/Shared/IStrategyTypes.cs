using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public interface IStrategyTypes
{
    Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken);
}
