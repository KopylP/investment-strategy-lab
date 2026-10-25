using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.Tests.FxRates;

public sealed class FxRateResolverTests
{
    private static FxRate EurPln(int month, int day, decimal rate) =>
        FxRate.Of(FxRateSource.Nbp, Currency.EUR, Currency.PLN, new DateOnly(2026, month, day), rate, $"{day:000}/A/NBP/2026");

    private static readonly FxRate[] Rates = [EurPln(4, 30, 4.25m), EurPln(5, 4, 4.26m), EurPln(5, 5, 4.27m)];

    [Fact]
    public void The_rate_of_the_last_business_day_before_the_transaction_is_used()
    {
        var resolved = FxRateResolver.Resolve(Rates, Currency.EUR, Currency.PLN, transactionDate: new DateOnly(2026, 5, 6));

        resolved.Value.Rate.ShouldBe(4.27m);
    }

    [Fact]
    public void The_rate_published_on_the_transaction_day_itself_is_not_used()
    {
        var resolved = FxRateResolver.Resolve(Rates, Currency.EUR, Currency.PLN, transactionDate: new DateOnly(2026, 5, 5));

        resolved.Value.EffectiveDate.ShouldBe(new DateOnly(2026, 5, 4));
    }

    [Fact]
    public void Holidays_without_a_table_fall_back_to_the_previous_published_rate()
    {
        // 1 and 3 May are Polish holidays, 2–3 May a weekend: no NBP table between 30 April and 4 May.
        var resolved = FxRateResolver.Resolve(Rates, Currency.EUR, Currency.PLN, transactionDate: new DateOnly(2026, 5, 4));

        resolved.Value.EffectiveDate.ShouldBe(new DateOnly(2026, 4, 30));
    }

    [Fact]
    public void A_rate_older_than_ten_days_is_refused()
    {
        var resolved = FxRateResolver.Resolve(Rates, Currency.EUR, Currency.PLN, transactionDate: new DateOnly(2026, 5, 16));

        resolved.Error.Code.ShouldBe("fx_rate.not_found");
    }

    [Fact]
    public void A_currency_against_itself_is_one()
    {
        var resolved = FxRateResolver.Resolve([], Currency.PLN, Currency.PLN, transactionDate: new DateOnly(2026, 5, 6));

        resolved.Value.Rate.ShouldBe(1m);
    }

    [Fact]
    public void Rates_of_another_currency_are_ignored()
    {
        var usd = FxRate.Of(FxRateSource.Nbp, Currency.USD, Currency.PLN, new DateOnly(2026, 5, 5), 3.70m, "085/A/NBP/2026");

        var resolved = FxRateResolver.Resolve([.. Rates, usd], Currency.USD, Currency.PLN, transactionDate: new DateOnly(2026, 5, 6));

        resolved.Value.Rate.ShouldBe(3.70m);
    }
}
