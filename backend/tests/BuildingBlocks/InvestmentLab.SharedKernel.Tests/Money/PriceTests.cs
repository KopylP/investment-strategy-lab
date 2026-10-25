namespace InvestmentLab.SharedKernel.Tests;

public sealed class PriceTests
{
    [Fact]
    public void Keeps_sub_cent_precision()
    {
        Price.Of(123.4567m, Currency.EUR).Amount.ShouldBe(123.4567m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Must_be_positive(decimal amount)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Price.Of(amount, Currency.EUR));
        Price.TryOf(amount, Currency.EUR).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Total_for_quantity_is_money()
    {
        Price.Of(10.125m, Currency.EUR).TotalFor(Quantity.Of(3m)).ShouldBe(Money.Of(30.38m, Currency.EUR));
    }
}
