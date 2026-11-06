using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Catalog.Infrastructure;

internal sealed class StrategyCatalog(CatalogSynchronizer synchronizer) : IStrategyCatalog
{
    public Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken) => synchronizer.ListAsync(cancellationToken);

    public Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken) => synchronizer.FindAsync(code, cancellationToken);

    public Task<int> SyncAsync(CancellationToken cancellationToken) => synchronizer.SyncAsync(cancellationToken);
}
