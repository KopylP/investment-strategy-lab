namespace InvestmentLab.SharedKernel.Tests;

public sealed class CurrencyTests
{
    [Fact]
    public void Of_normalizes_to_upper_case()
    {
        Currency.Of("eur").ShouldBe(Currency.EUR);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void TryOf_rejects_codes_that_are_not_three_letters(string code)
    {
        Currency.TryOf(code).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Of_throws_for_invalid_code()
    {
        Should.Throw<ArgumentException>(() => Currency.Of("12"));
    }

    [Fact]
    public void Same_code_means_same_currency()
    {
        Currency.Of("PLN").ShouldBe(Currency.PLN);
        Currency.PLN.ToString().ShouldBe("PLN");
    }
}
