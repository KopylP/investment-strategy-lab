namespace InvestmentLab.SharedKernel.Tests;

public sealed class PercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.19)]
    [InlineData(1)]
    public void Of_accepts_fractions_between_zero_and_one(decimal fraction)
    {
        Percentage.Of(fraction).Value.ShouldBe(fraction);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Of_rejects_fractions_outside_zero_and_one(decimal fraction)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Percentage.Of(fraction));
        Percentage.TryOf(fraction).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void FromPercent_divides_by_hundred()
    {
        Percentage.FromPercent(19).ShouldBe(Percentage.Of(0.19m));
    }

    [Fact]
    public void Named_values()
    {
        Percentage.Zero.Value.ShouldBe(0m);
        Percentage.Full.Value.ShouldBe(1m);
    }

    [Fact]
    public void Applies_to_money()
    {
        Percentage.FromPercent(19).Of(Money.Of(200m, Currency.PLN)).ShouldBe(Money.Of(38m, Currency.PLN));
    }

    [Fact]
    public void Signed_percentage_allows_losses()
    {
        var loss = SignedPercentage.Of(-0.25m);

        loss.Value.ShouldBe(-0.25m);
        loss.IsNegative.ShouldBeTrue();
        SignedPercentage.FromPercent(12.5m).Value.ShouldBe(0.125m);
    }
}
