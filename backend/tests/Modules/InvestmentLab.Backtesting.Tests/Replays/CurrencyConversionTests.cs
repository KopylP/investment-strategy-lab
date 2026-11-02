using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class CurrencyConversionTests
{
    private static readonly DateOnly Monday = new(2008, 9, 15);

    [Fact]
    public void Each_price_is_converted_at_the_latest_rate_known_on_its_day()
    {
        PricedDay[] prices = [new(Monday, 100m), new(Monday.AddDays(1), 110m), new(Monday.AddDays(2), 120m)];
        DatedRate[] rates = [new(Monday, 0.7m), new(Monday.AddDays(2), 0.8m)];

        var converted = CurrencyConversion.Converted(prices, rates);

        converted.Select(price => price.Price).ShouldBe([70m, 77m, 96m]);
    }

    [Fact]
    public void A_price_before_any_known_rate_has_no_value_in_the_replay_currency()
    {
        PricedDay[] prices = [new(Monday, 100m), new(Monday.AddDays(1), 110m)];
        DatedRate[] rates = [new(Monday.AddDays(1), 0.5m)];

        var converted = CurrencyConversion.Converted(prices, rates);

        converted.ShouldBe([new PricedDay(Monday.AddDays(1), 55m)]);
    }
}
