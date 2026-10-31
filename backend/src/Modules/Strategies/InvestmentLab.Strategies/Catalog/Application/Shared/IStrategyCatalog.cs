using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Catalog.Application.Shared;

public interface IStrategyCatalog
{
    Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken);

    Task<int> SyncAsync(CancellationToken cancellationToken);
}
