using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IPlanSettingsRepository
{
    Task<PlanSettings?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(PlanSettings settings);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
