namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record ValuedSubPortfolio(
    SubPortfolioHoldings Holdings,
    decimal Cash,
    IReadOnlyList<ValuedPosition> Positions,
    decimal Share)
{
    public decimal Value => Positions.Sum(position => position.Value) + Cash;

    public decimal CostBasis => Positions.Sum(position => position.CostBasis);

    public decimal Result => Positions.Sum(position => position.Result);
}
