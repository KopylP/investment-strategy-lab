using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class PriceTableTests
{
    [Fact]
    public void Marks_can_carry_over_a_holiday_but_trades_need_an_actual_quote()
    {
        var fund = Guid.NewGuid();
        var benchmark = Guid.NewGuid();
        var friday = new DateOnly(2026, 11, 6);
        var monday = friday.AddDays(3);
        var table = new PriceTable(new Dictionary<Guid, IReadOnlyList<PricedDay>>
        {
            [fund] = [new(friday, 100m)],
            [benchmark] = [new(friday, 200m), new(monday, 202m)],
        });
        table.PricesOn(1)[fund].ShouldBe(100m);
        table.TradablePricesOn(0)[fund].ShouldBe(100m);
        table.TradablePricesOn(1).ContainsKey(fund).ShouldBeFalse();
    }
}
