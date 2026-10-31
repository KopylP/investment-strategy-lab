namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record HoldingValuation(decimal Quantity, decimal? Close, decimal AverageCost)
{
    public decimal Value => Quantity * (Close ?? AverageCost);
}
