using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Application;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyTypes(CatalogSynchronizer synchronizer) : IStrategyTypes
{
    public async Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken) =>
        (await synchronizer.ListAsync(cancellationToken)).FirstOrDefault(type => type.Code == code);
}
