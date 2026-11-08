namespace InvestmentLab.SharedKernel.Tests.Text;

public sealed class LanguageTests
{
    [Theory]
    [InlineData("uk")]
    [InlineData("en")]
    [InlineData(" EN ")]
    public void A_language_the_service_speaks_is_known_by_its_code(string code)
    {
        Language.TryOf(code).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_language_the_service_does_not_speak_is_refused()
    {
        Language.TryOf("pl").Error.Code.ShouldBe("language.unsupported");
    }

    [Theory]
    [InlineData("uk-UA", "uk")]
    [InlineData("en-GB", "en")]
    [InlineData("pl", "en")]
    [InlineData("", "en")]
    public void A_culture_maps_to_its_language_or_to_the_default(string culture, string expected)
    {
        Language.OfCultureOrDefault(culture).Code.ShouldBe(expected);
    }
}
