using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Tests.Holdings;

public sealed class HoldingsValuationTests
{
    private static readonly DateOnly Day = new(2026, 11, 12);
    private static readonly Guid Etf = Guid.NewGuid(), Other = Guid.NewGuid();
    private static readonly SubPortfolioId Main = SubPortfolioId.New(), Second = SubPortfolioId.New();

    private static HoldingLine Line(Guid instrument, decimal quantity, decimal unitCost) =>
        new(instrument, [new VirtualLot(TransactionId.New(), Day, Quantity.Of(quantity), unitCost)]);

    private static SubPortfolioHoldings Sub(SubPortfolioId id, decimal cash, HoldingLine[] lines, params Money[] foreign) =>
        new(id, Money.Of(cash, Currency.EUR), foreign, lines);

    private static AccountHoldings Account(params SubPortfolioHoldings[] subs) => new(AccountId.New(), Currency.EUR, null, subs);

    private static HoldingsMarket Market(Dictionary<Guid, MarketPrice>? prices = null, Dictionary<Currency, decimal>? rates = null) =>
        new(prices ?? [], rates ?? []);

    private static MarketPrice Price(decimal quoted, decimal rate = 1m) => MarketPrice.Of(Day, quoted, Currency.EUR, rate, isStale: false);

    [Fact]
    public void A_position_is_worth_its_quantity_at_the_latest_price_in_the_account_currency()
    {
        var holdings = Account(Sub(Main, 0m, [Line(Etf, 2.935354m, 90m)]));

        var position = HoldingsValuation.Of(holdings, Market(new() { [Etf] = Price(95.24m) })).SubPortfolios[0].Positions[0];

        (position.Value, position.Result).ShouldBe((2.935354m * 95.24m, 2.935354m * 95.24m - position.CostBasis));
    }

    [Fact]
    public void A_price_quoted_in_another_currency_is_converted_at_the_rate_to_the_account_currency()
    {
        var holdings = Account(Sub(Main, 0m, [Line(Etf, 2m, 100m)]));

        var position = HoldingsValuation.Of(holdings, Market(new() { [Etf] = Price(120m, rate: 0.9m) })).SubPortfolios[0].Positions[0];

        position.Value.ShouldBe(216m);
    }

    [Fact]
    public void A_position_without_a_price_keeps_what_was_paid_for_it()
    {
        var holdings = Account(Sub(Main, 0m, [Line(Etf, 4m, 25m)]));

        var position = HoldingsValuation.Of(holdings, Market()).SubPortfolios[0].Positions[0];

        (position.Value, position.Result, position.Price).ShouldBe((100m, 0m, null));
    }

    [Fact]
    public void Cash_in_another_currency_counts_at_the_rate_to_the_account_currency()
    {
        var holdings = Account(Sub(Main, 10m, [], Money.Of(5m, Currency.USD)));

        var sub = HoldingsValuation.Of(holdings, Market(rates: new() { [Currency.USD] = 0.86m })).SubPortfolios[0];

        (sub.Cash, sub.Value).ShouldBe((14.3m, 14.3m));
    }

    [Fact]
    public void Weights_are_shares_of_the_sub_portfolio_and_sub_portfolio_shares_are_shares_of_the_account()
    {
        var holdings = Account(
            Sub(Main, 20m, [Line(Etf, 3m, 10m), Line(Other, 1m, 50m)]),
            Sub(Second, 100m, []));
        var market = Market(new() { [Etf] = Price(10m), [Other] = Price(50m) });

        var valued = HoldingsValuation.Of(holdings, market);

        valued.SubPortfolios[0].Positions.Select(position => position.Weight).ShouldBe([0.5m, 0.3m]);
        valued.SubPortfolios.Select(sub => sub.Share).ShouldBe([0.5m, 0.5m]);
    }

    [Fact]
    public void The_account_adds_up_its_sub_portfolios_and_its_result_is_a_share_of_what_was_paid()
    {
        var holdings = Account(
            Sub(Main, 5m, [Line(Etf, 2m, 10m)]),
            Sub(Second, 1m, [Line(Other, 1m, 30m)]));
        var market = Market(new() { [Etf] = Price(12m), [Other] = Price(27m) });

        var valued = HoldingsValuation.Of(holdings, market);

        (valued.Value, valued.Cash, valued.CostBasis, valued.Result, valued.ResultShare).ShouldBe((57m, 6m, 50m, 1m, 0.02m));
    }
}
