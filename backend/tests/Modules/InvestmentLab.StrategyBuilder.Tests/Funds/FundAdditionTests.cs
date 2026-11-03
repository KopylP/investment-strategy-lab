using InvestmentLab.StrategyBuilder.Funds.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Funds;

public sealed class FundAdditionTests
{
    [Fact]
    public void A_batch_cannot_take_the_draft_above_sixty_funds()
    {
        var selected = Enumerable.Range(0, 59).Select(_ => Guid.NewGuid()).ToList();
        var funds = new[] { Fund("A"), Fund("B") };

        FundAddition.TryOf(funds, selected).Error.Code.ShouldBe("builder.funds.too_many_funds");
    }

    [Fact]
    public void Known_funds_already_selected_do_not_consume_another_slot()
    {
        var selected = Enumerable.Range(0, 60).Select(_ => Guid.NewGuid()).ToList();
        var funds = new[] { Fund("A") with { InstrumentId = selected[0] } };

        FundAddition.TryOf(funds, selected).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Repeated_symbols_are_added_once()
    {
        var result = FundAddition.TryOf([Fund("aaa"), Fund("AAA")], []).Value;

        result.Funds.ShouldHaveSingleItem();
    }

    private static FoundFund Fund(string symbol) => new(symbol, "Fund", "EUR", null, null, null, null);
}
