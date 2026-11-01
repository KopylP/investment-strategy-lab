namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IAccountReports
{
    Task<IReadOnlyList<AccountReport>> OfAsync(Guid accountId, PlanSettings? settings, CancellationToken cancellationToken);
}
