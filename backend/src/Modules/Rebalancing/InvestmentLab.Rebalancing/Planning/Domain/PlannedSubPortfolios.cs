namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class PlannedSubPortfolios
{
    public static IEnumerable<IPlanningSubPortfolio> Of(IPlanningAccount account) =>
        account.SubPortfolios.Where(sub => sub.IsActive && sub.StrategyId is not null);
}
