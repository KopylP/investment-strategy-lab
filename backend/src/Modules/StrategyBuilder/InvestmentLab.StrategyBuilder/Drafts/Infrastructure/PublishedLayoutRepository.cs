using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class PublishedLayoutRepository(StrategyBuilderDbContext db) : IPublishedLayoutRepository
{
    public Task<PublishedLayout?> FindAsync(Guid strategyId, int version, CancellationToken cancellationToken) =>
        db.PublishedLayouts.FirstOrDefaultAsync(layout => layout.StrategyId == strategyId && layout.Version == version, cancellationToken);

    public void Add(PublishedLayout layout) => db.PublishedLayouts.Add(layout);
}
