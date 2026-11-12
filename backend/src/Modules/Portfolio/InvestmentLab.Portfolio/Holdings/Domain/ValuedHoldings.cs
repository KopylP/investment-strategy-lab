namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record ValuedHoldings(AccountHoldings Holdings, IReadOnlyList<ValuedSubPortfolio> SubPortfolios)
{
    public decimal Value => SubPortfolios.Sum(sub => sub.Value);

    public decimal Cash => SubPortfolios.Sum(sub => sub.Cash);

    public decimal CostBasis => SubPortfolios.Sum(sub => sub.CostBasis);

    public decimal Result => SubPortfolios.Sum(sub => sub.Result);

    public decimal? ResultShare => CostBasis == 0m ? null : Result / CostBasis;
}
