namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record ValuedPosition(HoldingLine Line, MarketPrice? Price, decimal Value, decimal Weight)
{
    public decimal CostBasis => Line.CostBasis;

    public decimal Result => Value - CostBasis;

    public decimal? ResultShare => CostBasis == 0m ? null : Value / CostBasis - 1m;
}
