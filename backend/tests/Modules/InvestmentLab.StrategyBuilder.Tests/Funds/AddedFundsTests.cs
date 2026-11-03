using InvestmentLab.StrategyBuilder.Funds.Domain;

namespace InvestmentLab.StrategyBuilder.Tests.Funds;

public sealed class AddedFundsTests
{
    [Fact]
    public void One_failed_registration_does_not_stop_the_others()
    {
        var first = Guid.NewGuid();
        var last = Guid.NewGuid();
        var added = new AddedFunds([], []);

        added = added.With("A", first, null);
        added = added.With("B", null, "failure");
        added = added.With("C", last, null);

        added.InstrumentIds.ShouldBe([first, last]);
        added.Failed.ShouldHaveSingleItem().ShouldBe(new FundNotAdded("B", "failure"));
    }

    [Fact]
    public void Adding_a_known_instrument_twice_returns_one_id()
    {
        var id = Guid.NewGuid();
        var added = new AddedFunds([], []).With("A", id, null).With("A", id, null);

        added.InstrumentIds.ShouldBe([id]);
    }

    [Fact]
    public void An_unexplained_registration_failure_gets_a_readable_reason()
    {
        var added = new AddedFunds([], []).With("A", null, null);

        added.Failed.ShouldHaveSingleItem().Reason.ShouldBe("@builder.funds.registration_failed");
    }
}
