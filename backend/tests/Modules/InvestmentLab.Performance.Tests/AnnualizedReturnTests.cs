using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Tests;

public sealed class AnnualizedReturnTests
{
    private static readonly DateOnly Start = new(2024, 1, 1);

    [Fact]
    public void A_return_over_less_than_a_year_is_not_annualized()
    {
        AnnualizedReturn.Of(0.10m, Start, Start.AddDays(300)).ShouldBeNull();
    }

    [Fact]
    public void A_return_over_two_years_is_the_yearly_rate_that_compounds_to_it()
    {
        var end = Start.AddDays(731);

        var annualized = AnnualizedReturn.Of(0.21m, Start, end);

        annualized.ShouldNotBeNull();
        annualized.Value.ShouldBe(0.1m, tolerance: 0.001m);
    }

    [Fact]
    public void A_loss_of_everything_is_a_yearly_loss_of_everything()
    {
        AnnualizedReturn.Of(-1m, Start, Start.AddDays(731)).ShouldBe(-1m);
    }

    [Fact]
    public void A_loss_beyond_everything_invested_has_no_annualized_return()
    {
        AnnualizedReturn.Of(-1.5m, Start, Start.AddDays(731)).ShouldBeNull();
    }
}
