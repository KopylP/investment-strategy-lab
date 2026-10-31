namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioStanding(Guid SubPortfolioId, Percentage TargetShare, decimal Value)
{
    public static SubPortfolioStanding Valued(Guid subPortfolioId, decimal targetShare, decimal cash, IEnumerable<HoldingValuation> holdings)
    {
        var value = cash + holdings.Sum(holding => holding.Value);

        return new SubPortfolioStanding(subPortfolioId, Percentage.Of(targetShare), value);
    }
}
