using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

public interface IPlanSettingsRepository
{
    Task<PlanSettings?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(PlanSettings settings);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
