namespace InvestmentLab.SharedKernel.Tests;

public sealed class QuantityTests
{
    [Fact]
    public void Supports_fractional_shares()
    {
        Quantity.Of(1.2345m).Value.ShouldBe(1.2345m);
    }

    [Fact]
    public void Cannot_be_negative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Quantity.Of(-1m));
        Quantity.TryOf(-1m).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void None_is_zero()
    {
        Quantity.None.IsNone.ShouldBeTrue();
        Quantity.Of(0m).ShouldBe(Quantity.None);
    }

    [Fact]
    public void Adds_and_subtracts()
    {
        (Quantity.Of(3m) + Quantity.Of(1.5m)).ShouldBe(Quantity.Of(4.5m));
        (Quantity.Of(3m) - Quantity.Of(1.5m)).ShouldBe(Quantity.Of(1.5m));
    }

    [Fact]
    public void Subtracting_more_than_available_is_rejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Quantity.Of(1m) - Quantity.Of(2m));
    }

    [Fact]
    public void Compares_quantities()
    {
        (Quantity.Of(2m) > Quantity.Of(1m)).ShouldBeTrue();
        (Quantity.Of(1m) < Quantity.Of(2m)).ShouldBeTrue();
        (Quantity.Of(2m) >= Quantity.Of(2m)).ShouldBeTrue();
        (Quantity.Of(2m) <= Quantity.Of(2m)).ShouldBeTrue();
    }
}
