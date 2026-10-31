namespace InvestmentLab.Rebalancing.Contracts;

public interface IReportQueries
{
    Task<int> GetOpenActionsCount(Guid accountId, CancellationToken cancellationToken);
}
