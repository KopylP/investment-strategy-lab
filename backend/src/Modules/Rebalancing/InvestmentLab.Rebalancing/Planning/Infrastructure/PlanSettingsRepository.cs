using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class PlanSettingsRepository(RebalancingDbContext db) : IPlanSettingsRepository
{
    public Task<PlanSettings?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
        db.PlanSettings.SingleOrDefaultAsync(settings => settings.Id == accountId, cancellationToken);

    public void Add(PlanSettings settings) => db.PlanSettings.Add(settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
