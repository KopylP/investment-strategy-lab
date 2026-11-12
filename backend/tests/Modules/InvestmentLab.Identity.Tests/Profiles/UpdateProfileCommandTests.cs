using InvestmentLab.Identity.Profiles.Application.UpdateProfile;
using InvestmentLab.Identity.Profiles.Domain;
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void No_chart_style_keeps_the_current_one(string? chartStyle)
    {
        var chosen = new UpdateProfileCommand("EUR", null, "Europe/Warsaw", chartStyle).ChosenChartStyle();

        chosen.Value.ShouldBeNull();
    }

    [Fact]
    public void A_chosen_chart_style_is_one_the_service_draws()
    {
        new UpdateProfileCommand("EUR", null, "Europe/Warsaw", "Candles").ChosenChartStyle().Value.ShouldBe(ChartStyle.Candles);
        new UpdateProfileCommand("EUR", null, "Europe/Warsaw", "Bars").ChosenChartStyle().IsFailure.ShouldBeTrue();
        new UpdateProfileCommand("EUR", null, "Europe/Warsaw", "42").ChosenChartStyle().IsFailure.ShouldBeTrue();
    }
}
