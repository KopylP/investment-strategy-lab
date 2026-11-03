using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Tests;

public sealed class SignalTargetTests
{
    [Fact]
    public void Only_instrument_and_group_signals_are_market_signals()
    {
        var accountId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();

        SignalTarget.Instrument(instrumentId).IsMarket.ShouldBeTrue();
        SignalTarget.InstrumentGroup("Equities").IsMarket.ShouldBeTrue();
        SignalTarget.EachPosition(accountId).IsMarket.ShouldBeFalse();
        SignalTarget.SubPortfolio(accountId, Guid.NewGuid()).IsMarket.ShouldBeFalse();
        SignalTarget.Account(accountId).IsMarket.ShouldBeFalse();
    }
}
