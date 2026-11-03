using InvestmentLab.StrategyBuilder.Funds.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Funds;

public sealed class FundCriteriaTests
{
    [Fact]
    public void A_search_needs_an_exchange_or_a_region()
    {
        FundCriteria.TryOf(null, " ", null, null, null, []).Error.Code.ShouldBe("builder.funds.place_required");
    }

    [Fact]
    public void At_most_five_words()
    {
        FundCriteria.TryOf("place", null, null, null, null, ["a", "b", "c", "d", "e", "f"])
            .Error.Code.ShouldBe("builder.funds.too_many_words");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0.11)]
    public void The_yearly_fee_must_be_between_zero_and_ten_percent(decimal expense)
    {
        FundCriteria.TryOf("place", null, null, null, expense, []).Error.Code.ShouldBe("builder.funds.expense_ratio");
    }

    [Fact]
    public void Assets_cannot_be_negative()
    {
        FundCriteria.TryOf("place", null, null, -1, null, []).Error.Code.ShouldBe("builder.funds.assets");
    }

    [Fact]
    public void Search_text_is_trimmed_and_empty_words_are_removed()
    {
        var criteria = FundCriteria.TryOf(" place ", null, " family ", 0, 0, [" world ", " "]).Value;

        criteria.ExchangeId.ShouldBe("place");
        criteria.FundFamily.ShouldBe("family");
        criteria.NameWords.ShouldBe(["world"]);
    }
}
