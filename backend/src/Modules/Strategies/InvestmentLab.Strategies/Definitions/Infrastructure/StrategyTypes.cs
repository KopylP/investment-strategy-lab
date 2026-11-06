using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyTypes(CatalogSynchronizer synchronizer) : IStrategyTypes
{
    public Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken) =>
        synchronizer.FindAsync(code, cancellationToken);

    public Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken) =>
        synchronizer.ListAsync(cancellationToken);
}
