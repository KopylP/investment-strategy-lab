namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IPlanningAccounts
{
    Task<IPlanningAccount?> FindAsync(Guid accountId, CancellationToken cancellationToken);
}
