using InvestmentLab.Strategies.Evaluation.Domain;

namespace InvestmentLab.Strategies.Tests.Evaluation;

public sealed class SignalStatesTests
{
    [Fact]
    public void Referenced_signals_take_their_known_states()
    {
        var id = Guid.NewGuid();
        var instrument = Guid.NewGuid();
        var state = new SignalState(id, [instrument]);

        SignalStates.For([id], [state]).Value.ShouldBe([state]);
    }

    [Fact]
    public void A_referenced_signal_without_a_state_refuses_the_evaluation()
    {
        SignalStates.For([Guid.NewGuid()], []).Error.Code.ShouldBe("strategy.signal_deleted");
    }

    [Fact]
    public void States_of_signals_the_rules_do_not_name_are_not_sent()
    {
        var state = new SignalState(Guid.NewGuid(), []);

        SignalStates.For([], [state]).Value.ShouldBeEmpty();
    }

    [Fact]
    public void An_existing_inactive_signal_has_a_known_empty_state()
    {
        var state = new SignalState(Guid.NewGuid(), []);

        SignalStates.For([state.SignalId], [state]).Value.ShouldBe([state]);
    }
}
