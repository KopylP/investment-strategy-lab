namespace InvestmentLab.SharedKernel.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Of_rounds_to_cents()
    {
        Money.Of(10.005m, Currency.EUR).Amount.ShouldBe(10.00m);
        Money.Of(10.015m, Currency.EUR).Amount.ShouldBe(10.02m);
    }

    [Fact]
    public void Adds_and_subtracts_in_same_currency()
    {
        var a = Money.Of(100m, Currency.EUR);
        var b = Money.Of(25.50m, Currency.EUR);

        (a + b).ShouldBe(Money.Of(125.50m, Currency.EUR));
        (a - b).ShouldBe(Money.Of(74.50m, Currency.EUR));
    }

    [Fact]
    public void Arithmetic_across_currencies_is_rejected()
    {
        var eur = Money.Of(1m, Currency.EUR);
        var usd = Money.Of(1m, Currency.USD);

        Should.Throw<CurrencyMismatchException>(() => eur + usd);
    }

    [Fact]
    public void Multiply_rounds_result()
    {
        Money.Of(10m, Currency.EUR).Multiply(0.3333m).Amount.ShouldBe(3.33m);
    }

    [Fact]
    public void Zero_has_no_amount()
    {
        var zero = Money.Zero(Currency.PLN);

        zero.Amount.ShouldBe(0m);
        zero.IsZero.ShouldBeTrue();
    }

    [Fact]
    public void Can_be_negative_for_outflows()
    {
        var money = Money.Of(-5m, Currency.EUR);

        money.IsNegative.ShouldBeTrue();
        money.Negate().ShouldBe(Money.Of(5m, Currency.EUR));
    }

    [Fact]
    public void Total_sums_amounts_of_one_currency()
    {
        IEnumerable<Money> amounts =
        [
            Money.Of(1.10m, Currency.EUR),
            Money.Of(2.20m, Currency.EUR),
        ];

        amounts.Total(Currency.EUR).ShouldBe(Money.Of(3.30m, Currency.EUR));
    }

    [Fact]
    public void Total_of_nothing_is_zero()
    {
        Array.Empty<Money>().Total(Currency.EUR)
            .ShouldBe(Money.Zero(Currency.EUR));
    }

    [Fact]
    public void Formats_amount_with_currency()
    {
        Money.Of(1234.5m, Currency.EUR).ToString().ShouldBe("1234.50 EUR");
    }
}
