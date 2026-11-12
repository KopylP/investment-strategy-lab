namespace InvestmentLab.Portfolio.Holdings.Domain;

public static class HoldingsValuation
{
    public static ValuedHoldings Of(AccountHoldings holdings, HoldingsMarket market)
    {
        var unshared = holdings.SubPortfolios.Select(sub => ValueOf(sub, market)).ToList();
        var total = unshared.Sum(sub => sub.Value);

        var subPortfolios = unshared.Select(sub => sub with { Share = ShareOf(sub.Value, total) }).ToList();

        return new ValuedHoldings(holdings, subPortfolios);
    }

    private static ValuedSubPortfolio ValueOf(SubPortfolioHoldings sub, HoldingsMarket market)
    {
        var cash = sub.Cash.Amount + sub.ForeignCash.Sum(market.InAccountCurrency);
        var unweighted = sub.Lines.Select(line => ValueOf(line, market)).ToList();
        var value = unweighted.Sum(position => position.Value) + cash;

        var positions = unweighted
            .Select(position => position with { Weight = ShareOf(position.Value, value) })
            .OrderByDescending(position => position.Value)
            .ToList();

        return new ValuedSubPortfolio(sub, cash, positions, Share: 0m);
    }

    private static ValuedPosition ValueOf(HoldingLine line, HoldingsMarket market)
    {
        var price = market.PriceOf(line.InstrumentId);
        var value = price is null ? line.CostBasis : line.Quantity.Value * price.InAccountCurrency;

        return new ValuedPosition(line, price, value, Weight: 0m);
    }

    private static decimal ShareOf(decimal part, decimal whole) => whole == 0m ? 0m : part / whole;
}
