namespace InvestmentLab.SharedKernel.Tests;

public sealed class IsinTests
{
    [Theory]
    [InlineData("IE00B5BMR087")]
    [InlineData("US0378331005")]
    [InlineData("LU0290358497")]
    public void Accepts_valid_isins(string value)
    {
        Isin.Of(value).Value.ShouldBe(value);
    }

    [Fact]
    public void Normalizes_case_and_whitespace()
    {
        Isin.Of(" ie00b5bmr087 ").Value.ShouldBe("IE00B5BMR087");
    }

    [Theory]
    [InlineData("IE00B5BMR088")]
    [InlineData("IE00B5BMR08")]
    [InlineData("1E00B5BMR087")]
    [InlineData("")]
    public void Rejects_invalid_isins(string value)
    {
        Isin.TryOf(value).IsFailure.ShouldBeTrue();
        Should.Throw<ArgumentException>(() => Isin.Of(value));
    }
}
