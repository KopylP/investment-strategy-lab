using InvestmentLab.Identity.Profiles.Application.UpdateProfile;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.Identity.Tests.Profiles;

public sealed class UpdateProfileCommandTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void No_language_means_the_app_follows_the_browser(string? language)
    {
        var chosen = new UpdateProfileCommand("EUR", language, "Europe/Warsaw").ChosenLanguage();

        chosen.Value.ShouldBeNull();
    }

    [Fact]
    public void A_chosen_language_is_one_the_service_speaks()
    {
        new UpdateProfileCommand("EUR", "en", "Europe/Warsaw").ChosenLanguage().Value.ShouldBe(Language.English);
        new UpdateProfileCommand("EUR", "de", "Europe/Warsaw").ChosenLanguage().IsFailure.ShouldBeTrue();
    }
}
