using InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

namespace InvestmentLab.Signals.Tests;

public sealed class InstrumentSubjectsTests
{
    [Fact]
    public void An_instrument_subject_key_gives_back_its_instrument()
    {
        var instrumentId = Guid.NewGuid();
        var key = InstrumentSubjects.KeyOf(instrumentId);

        InstrumentSubjects.InstrumentIdOf(key).ShouldBe(instrumentId);
    }

    [Theory]
    [InlineData("account")]
    [InlineData("instrument:not-an-id")]
    [InlineData("")]
    public void Another_subject_key_has_no_instrument(string key)
    {
        InstrumentSubjects.InstrumentIdOf(key).ShouldBeNull();
    }
}
