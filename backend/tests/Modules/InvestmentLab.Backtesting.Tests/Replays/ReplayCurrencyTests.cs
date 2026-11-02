using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class ReplayCurrencyTests
{
    [Fact]
    public void A_strategy_is_replayed_in_the_currency_most_of_its_funds_trade_in()
    {
        ReplayCurrency.Of(["USD", "EUR", "eur"]).ShouldBe("EUR");
    }

    [Fact]
    public void A_tie_goes_to_the_currency_of_the_first_fund()
    {
        ReplayCurrency.Of(["USD", "EUR"]).ShouldBe("USD");
    }

    [Fact]
    public void A_strategy_without_funds_fails_loudly()
    {
        Should.Throw<ArgumentException>(() => ReplayCurrency.Of([]));
    }
}
