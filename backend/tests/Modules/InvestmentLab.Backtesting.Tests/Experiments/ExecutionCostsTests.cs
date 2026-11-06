using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ExecutionCostsTests
{
    private static readonly Guid InstrumentId = Guid.NewGuid();
    private static readonly DateOnly Day = new(2025, 1, 2);

    [Fact]
    public void Both_sides_pay_half_the_spread_and_adverse_slippage()
    {
        var costs = new TradingCosts(SpreadBps: 20m, SlippageBps: 10m);

        costs.FillPrice(100m, isSale: false).ShouldBe(100.2m);
        costs.FillPrice(100m, isSale: true).ShouldBe(99.8m);
        costs.SpreadCostOf(10m, 100m).ShouldBe(1m);
        costs.SlippageCostOf(10m, 100m).ShouldBe(1m);
    }

    [Fact]
    public void A_purchase_reserves_fixed_and_proportional_commission_before_sizing()
    {
        var costs = new TradingCosts(CommissionRate: .01m, FeePerTrade: 10m);
        var book = new ReplayBook(1020m, costs);

        book.Buy(InstrumentId, 2000m, 100m, Day);

        book.Cash.ShouldBe(0m);
        var trade = book.TradeLog.ShouldHaveSingleItem();
        trade.Quantity.ShouldBe(10m);
        trade.Fee.ShouldBe(20m);
    }

    [Fact]
    public void A_purchase_that_cannot_cover_the_fixed_fee_is_not_executed()
    {
        var book = new ReplayBook(5m, new TradingCosts(FeePerTrade: 10m));

        book.Buy(InstrumentId, 100m, 100m, Day);

        book.TradeLog.ShouldBeEmpty();
        book.Cash.ShouldBe(5m);
    }

    [Fact]
    public void A_sale_records_execution_friction_and_deducts_commission_from_proceeds()
    {
        var costs = new TradingCosts(CommissionRate: .01m, FeePerTrade: 1m, SpreadBps: 20m, SlippageBps: 10m);
        var book = new ReplayBook(2000m, costs);
        book.Buy(InstrumentId, 1002m, 100m, Day);
        var cashBeforeSale = book.Cash;

        book.Sell(InstrumentId, 10m, 100m, Day.AddDays(1));

        var sale = book.TradeLog[^1];
        sale.Price.ShouldBe(99.8m);
        sale.ReferencePrice.ShouldBe(100m);
        sale.SpreadCost.ShouldBe(1m);
        sale.SlippageCost.ShouldBe(1m);
        sale.Fee.ShouldBe(10.98m);
        book.Cash.ShouldBe(cashBeforeSale + 987.02m);
        book.Positions.ShouldBeEmpty();
    }
}
