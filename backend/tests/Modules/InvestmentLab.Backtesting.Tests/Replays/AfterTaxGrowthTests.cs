using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class AfterTaxGrowthTests
{
    private const decimal Capital = 1000m;

    [Fact]
    public void A_tax_paid_at_the_turn_of_the_year_shrinks_everything_that_grows_after_it()
    {
        GrowthDay[] days = [new(new DateOnly(2024, 12, 30), 1.0), new(new DateOnly(2025, 1, 2), 1.2), new(new DateOnly(2025, 12, 30), 2.4)];

        var final = AfterTaxGrowth.FinalOf(days, [new YearTax(2024, 120m)], Capital);

        final.ShouldBe(2.4 * (1 - 120.0 / 1200.0), 1e-9);
    }

    [Fact]
    public void The_tax_of_the_last_year_is_paid_out_of_the_final_value()
    {
        GrowthDay[] days = [new(new DateOnly(2025, 1, 2), 1.0), new(new DateOnly(2025, 12, 30), 1.5)];

        var final = AfterTaxGrowth.FinalOf(days, [new YearTax(2025, 95m)], Capital);

        final.ShouldBe(1.5 - 0.095, 1e-9);
    }

    [Fact]
    public void A_fund_held_throughout_is_bought_on_the_first_day_and_sold_on_the_last()
    {
        var fund = Guid.NewGuid();
        GrowthDay[] growth = [new(new DateOnly(2020, 1, 2), 1.0), new(new DateOnly(2025, 12, 30), 3.0)];

        var trades = BookTrade.HeldThroughout(fund, growth, Capital);

        trades.Select(trade => (trade.IsSale, trade.Quantity * trade.Price)).ShouldBe([(false, 1000m), (true, 3000m)]);
    }

    [Fact]
    public void The_book_logs_every_trade_and_can_be_sold_out_on_paper()
    {
        var fund = Guid.NewGuid();
        var book = new ReplayBook(1000m);
        var day = new DateOnly(2025, 3, 3);

        book.Buy(fund, 500m, 10m, day);
        book.Sell(fund, 20m, 12m, day.AddDays(30));
        var liquidation = book.LiquidationOn(day.AddDays(60), new Dictionary<Guid, decimal> { [fund] = 15m });

        book.TradeLog.Select(trade => trade.IsSale).ShouldBe([false, true]);
        liquidation.Single().Quantity.ShouldBe(book.Positions.Single().Quantity);
        liquidation.Single().Price.ShouldBe(15m);
    }
}
